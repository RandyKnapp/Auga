// APIManager by blaxxun (https://github.com/blaxxun-boop/APIManager), upstream commit 289ad9c, compiled into Auga
// as source instead of being built separately and merged in with ILRepack.
//
// Two local changes, both marked in place:
//   * the class is internal, as the ILRepack merge used to make it, so mods do not see it on Auga.dll;
//   * nested-types.patch (in this folder): upstream redirects the types nested in a mod's embedded copy of the
//     Auga API (its closures and iterators) to the outer Auga type and rewrites method definitions, which
//     corrupts the assembly when Cecil writes it ("Failed patching ... InvalidCastException" at load for mods
//     that embed the old Auga API shim).
// To update: replace this file with the new upstream Patcher.cs, re-apply nested-types.patch, make the class
// internal again and keep this header.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Preloader;
using BepInEx.Preloader.Patching;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;
using CustomAttributeNamedArgument = Mono.Cecil.CustomAttributeNamedArgument;
using ICustomAttributeProvider = Mono.Cecil.ICustomAttributeProvider;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace APIManager;

// internal (upstream: public), see the header
internal static class Patcher
{
	private static PluginInfo? lastPluginInfo;
	private static bool modifyNextLoad = false;
	private static string modGUID = null!;
	private static HashSet<string> redirectedNamespaces = null!;
	private static readonly Assembly patchingAssembly = Assembly.GetExecutingAssembly();
	private static string currentAssemblyPath = null!;

	private static readonly string dumpedAssembliesPath = (string)AccessTools.DeclaredField(typeof(AssemblyPatcher), "DumpedAssembliesPath").GetValue(null);

	private static void GrabPluginInfo(PluginInfo __instance) => lastPluginInfo = __instance;

	[HarmonyPriority(Priority.VeryHigh)]
	private static void CheckAssemblyLoadFile(string __0)
	{
		if (__0 == lastPluginInfo?.Location && lastPluginInfo.Dependencies.Any(d => d.DependencyGUID == modGUID))
		{
			modifyNextLoad = true;
			currentAssemblyPath = __0;
		}

		lastPluginInfo = null;
	}

	[HarmonyPriority(Priority.HigherThanNormal)]
	private static bool InterceptAssemblyLoadFile(string __0, ref Assembly? __result)
	{
		if (modifyNextLoad && __result is null)
		{
			string dumpedAssemblyPath = dumpedAssembliesPath + Path.DirectorySeparatorChar + Path.GetFileName(__0);
			if (File.Exists(dumpedAssemblyPath))
			{
				using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(dumpedAssemblyPath, new ReaderParameters { AssemblyResolver = new MonoAssemblyResolver() });

				bool foundGuid = false;
				foreach (CustomAttribute customAttribute in assembly.CustomAttributes)
				{
					TypeReference attributeType = customAttribute.Constructor.DeclaringType;
					if (attributeType.Namespace == "APIManager" && attributeType.Name == "PatchedAttribute")
					{
						string guid = (string)customAttribute.ConstructorArguments[0].Value;
						if (guid == modGUID)
						{
							foundGuid = (string)customAttribute.ConstructorArguments[1].Value == patchingAssembly.ManifestModule.ModuleVersionId.ToString();
							break;
						}
					}
				}

				if (foundGuid)
				{
					return true;
				}
			}

			__result = Assembly.Load(File.ReadAllBytes(__0));
			return false;
		}
		return true;
	}

	[HarmonyPriority(Priority.HigherThanNormal - 1)]
	private static bool ReplaceAssemblyLoadWithCache(ref string __0, ref Assembly? __result)
	{
		if (modifyNextLoad && __result is null && !__0.StartsWith(dumpedAssembliesPath))
		{
			string dumpedAssemblyPath = dumpedAssembliesPath + Path.DirectorySeparatorChar + Path.GetFileName(__0);

			AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(dumpedAssemblyPath, new ReaderParameters { AssemblyResolver = new MonoAssemblyResolver() });
			AssemblyDefinition originalAssembly = AssemblyDefinition.ReadAssembly(__0, new ReaderParameters { AssemblyResolver = new MonoAssemblyResolver() });

			foreach (CustomAttribute customAttribute in assembly.CustomAttributes)
			{
				TypeReference attributeType = customAttribute.Constructor.DeclaringType;
				if (attributeType.Namespace == "APIManager" && attributeType.Name == "PatchedAttribute")
				{
					string guid = (string)customAttribute.ConstructorArguments[0].Value;
					if (guid == "" ? (string)customAttribute.ConstructorArguments[1].Value != originalAssembly.MainModule.Mvid.ToString() : !BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(guid))
					{
						originalAssembly.Dispose();
						assembly.Dispose();

						__result = Assembly.Load(File.ReadAllBytes(__0));
						return false;
					}
				}
			}

			((Dictionary<string, string>)typeof(EnvVars).Assembly.GetType("BepInEx.Preloader.RuntimeFixes.UnityPatches").GetProperty("AssemblyLocations")!.GetValue(null))[assembly.FullName] = __0;

			originalAssembly.Dispose();
			assembly.Dispose();

			__0 = dumpedAssemblyPath;
		}
        
        modifyNextLoad = false;
		return true;
	}

	private class MonoAssemblyResolver : IAssemblyResolver
	{
		public AssemblyDefinition Resolve(AssemblyNameReference name)
		{
			return AssemblyDefinition.ReadAssembly(AppDomain.CurrentDomain.Load(name.FullName).Location);
		}

		public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
		{
			return Resolve(name);
		}

		public void Dispose() { }
	}

	private class AssemblyLoadInterceptor
	{
		private static MethodInfo TargetMethod() => AccessTools.DeclaredMethod(typeof(Assembly), nameof(Assembly.Load), new[] { typeof(byte[]) });
		private static string? assemblyPath = null;

		private static bool Prefix(ref byte[] __0, ref Assembly? __result)
		{
			assemblyPath = null;
			if (modifyNextLoad)
			{
				modifyNextLoad = false;

				try
				{
					using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(new MemoryStream(__0), new ReaderParameters { AssemblyResolver = new MonoAssemblyResolver() });

					((Dictionary<string, string>)typeof(EnvVars).Assembly.GetType("BepInEx.Preloader.RuntimeFixes.UnityPatches").GetProperty("AssemblyLocations")!.GetValue(null))[assembly.FullName] = currentAssemblyPath;

					FixupModuleReferences(assembly.MainModule);

					if (assembly.MainModule.GetType("APIManager", "PatchedAttribute") is not { } type)
					{
						type = new TypeDefinition("APIManager", "PatchedAttribute", TypeAttributes.NestedPrivate)
						{
							BaseType = assembly.MainModule.ImportReference(typeof(Attribute)),
						};
						MethodDefinition ctor = new(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, assembly.MainModule.TypeSystem.Void);
						ctor.Parameters.Add(new ParameterDefinition(assembly.MainModule.TypeSystem.String));
						ctor.Parameters.Add(new ParameterDefinition(assembly.MainModule.TypeSystem.String));
						ctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
						type.Methods.Add(ctor);
						assembly.MainModule.Types.Add(type);

						CustomAttribute ownAttr = new(ctor);
						ownAttr.ConstructorArguments.Add(new CustomAttributeArgument(assembly.MainModule.TypeSystem.String, ""));
						ownAttr.ConstructorArguments.Add(new CustomAttributeArgument(assembly.MainModule.TypeSystem.String, assembly.MainModule.Mvid.ToString()));
						assembly.CustomAttributes.Add(ownAttr);
					}

					MethodDefinition attrCtor = type.Methods.First(m => m.Name == ".ctor");
					CustomAttribute attr = new(attrCtor);
					attr.ConstructorArguments.Add(new CustomAttributeArgument(assembly.MainModule.TypeSystem.String, modGUID));
					attr.ConstructorArguments.Add(new CustomAttributeArgument(assembly.MainModule.TypeSystem.String, patchingAssembly.ManifestModule.ModuleVersionId.ToString()));
					assembly.CustomAttributes.Add(attr);

					using MemoryStream stream = new();
					assembly.Write(stream);
					__0 = stream.ToArray();

					string dumpedAssemblyPath = dumpedAssembliesPath + Path.DirectorySeparatorChar + assembly.Name.Name + ".dll";
					Directory.CreateDirectory(dumpedAssembliesPath);
					File.WriteAllBytes(dumpedAssemblyPath, __0);

					// skip main assembly load code
					assemblyPath = dumpedAssemblyPath;
					__result = null;
					return false;
				}
				catch (BadImageFormatException)
				{
					// No chance, nothing we can do here
				}
				catch (Exception e)
				{
					Debug.LogError("Failed patching ... " + e);
				}
			}

			return true;
		}

		private static void Postfix(ref Assembly? __result)
		{
			if (assemblyPath is not null && __result == null /* successfully skipped */)
			{
				__result = Assembly.LoadFrom(assemblyPath);
				assemblyPath = null;
			}
		}
	}

	private static void FixupModuleReferences(ModuleDefinition module)
	{
		TypeReference baseDeclaringType(TypeReference type)
		{
			while (type.DeclaringType is not null)
			{
				type = type.DeclaringType;
			}
			return type;
		}

		void Dispatch(TypeDefinition type)
		{
			if (type.BaseType is not null)
			{
				if (type.BaseType.Scope == module && redirectedNamespaces.Contains(baseDeclaringType(type.BaseType).Namespace))
				{
					if (patchingAssembly.GetType(type.BaseType.FullName) is { } originalType)
					{
						type.BaseType = module.ImportReference(originalType);
					}
				}
			}

			DispatchGenericParameters(type, type.FullName);
			DispatchInterfaces(type, type.FullName);
			DispatchAttributes(type, type.FullName);
			DispatchFields(type, type.FullName);
			DispatchProperties(type, type.FullName);
			DispatchEvents(type, type.FullName);
			DispatchMethods(type);
		}

		void DispatchGenericParameters(IGenericParameterProvider provider, string referencingEntityName)
		{
			foreach (GenericParameter? parameter in provider.GenericParameters)
			{
				DispatchAttributes(parameter, referencingEntityName);

				for (int i = 0; i < parameter.Constraints.Count; i++)
				{
					parameter.Constraints[i] = VisitType(parameter.Constraints[i], referencingEntityName);
				}
			}
		}

		void DispatchMethods(TypeDefinition type)
		{
			foreach (MethodDefinition? method in type.Methods)
			{
				DispatchMethod(method);
			}
		}

		bool AreSame(TypeReference a, TypeReference b) => (bool)typeof(MetadataResolver).Assembly.GetType("Mono.Cecil.MetadataResolver").GetMethod("AreSame", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(TypeReference), typeof(TypeReference) }, null)!.Invoke(null, new object[] { a, b });

		MethodReference? importMethodReference(MethodReference method)
		{
			bool CompareMethods(MethodBase m)
			{
				ParameterInfo[] mParams = m.GetParameters();
				if (method.IsGenericInstance != m.IsGenericMethodDefinition || (mParams.Length > 0) != method.HasParameters)
				{
					return false;
				}

				if (method.HasParameters)
				{
					if (method.Parameters.Count != mParams.Length)
					{
						return false;
					}

					for (int i = 0; i < method.Parameters.Count; ++i)
					{
						if (mParams[i].ParameterType.IsGenericParameter ? !method.Parameters[i].ParameterType.IsGenericParameter : !AreSame(method.Parameters[i].ParameterType, module.ImportReference(mParams[i].ParameterType)))
						{
							return false;
						}
					}
				}

				return true;
			}

			if (method.DeclaringType.Scope == module && redirectedNamespaces.Contains(baseDeclaringType(method.DeclaringType).Namespace))
			{
				if (method.Name == ".cctor")
				{
					if (patchingAssembly.GetType(method.DeclaringType.FullName)?.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) is { Length: 1 } staticCtor)
					{
						return module.ImportReference(staticCtor[0]);
					}
				}
				else if (method.Name == ".ctor")
				{
					if (patchingAssembly.GetType(method.DeclaringType.FullName)?.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(CompareMethods) is { } ctor)
					{
						return module.ImportReference(ctor);
					}
				}
				else
				{
					if (patchingAssembly.GetType(method.DeclaringType.FullName)?.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(m =>
					    {
						    if (m.Name != method.Name)
						    {
							    return false;
						    }

						    if (method.ReturnType.ContainsGenericParameter != m.ReturnType.IsGenericParameter && !AreSame(method.ReturnType, module.ImportReference(m.ReturnType)))
						    {
							    return false;
						    }

						    return CompareMethods(m);
					    }) is { } match)
					{
						MethodReference import = module.ImportReference(match);
						if (method is GenericInstanceMethod generic)
						{
							GenericInstanceMethod genericImport = new(import);
							for (int i = 0; i < generic.GenericArguments.Count; ++i)
							{
								generic.GenericArguments[i] = VisitType(generic.GenericArguments[i], method.FullName);
								genericImport.GenericArguments.Add(generic.GenericArguments[i]);
							}

							import = genericImport;
						}

						return import;
					}
				}
			}

			return null;
		}

		FieldReference? importFieldReference(FieldReference field)
		{
			if (field.DeclaringType.Scope == module && redirectedNamespaces.Contains(baseDeclaringType(field.DeclaringType).Namespace))
			{
				if (patchingAssembly.GetType(field.DeclaringType.FullName)?.GetField(field.Name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) is { } fieldInfo)
				{
					return module.ImportReference(fieldInfo);
				}
			}

			return null;
		}

		void DispatchMethod(MethodDefinition method)
		{
			method.ReturnType = VisitType(method.ReturnType, method.FullName);
			DispatchAttributes(method.MethodReturnType, method.FullName);
			DispatchGenericParameters(method, method.FullName);

			foreach (ParameterDefinition? parameter in method.Parameters)
			{
				parameter.ParameterType = VisitType(parameter.ParameterType, method.FullName);
				DispatchAttributes(parameter, method.FullName);
			}

			for (int i = 0; i < method.Overrides.Count; ++i)
			{
				if (importMethodReference(method.Overrides[i]) is { } reference)
				{
					method.Overrides[i] = reference;
				}
				else
				{
					VisitMethod(method.Overrides[i], method.FullName);
				}
			}

			if (method.HasBody)
			{
				DispatchMethodBody(method.Body);
			}
		}

		void DispatchMethodBody(MethodBody body)
		{
			foreach (VariableDefinition variable in body.Variables)
			{
				variable.VariableType = VisitType(variable.VariableType, body.Method.FullName);
			}

			foreach (Instruction instruction in body.Instructions)
			{
				switch (instruction.Operand)
				{
					case FieldReference field:
						if (importFieldReference(field) is { } fieldReference)
						{
							instruction.Operand = fieldReference;
						}
						else
						{
							VisitField(field, body.Method.FullName);
						}
						break;
					case MethodReference method:
						if (importMethodReference(method) is { } reference)
						{
							instruction.Operand = reference;
						}
						else
						{
							VisitMethod(method, body.Method.FullName);
						}
						break;
					case TypeReference type:
						instruction.Operand = VisitType(type, body.Method.FullName);
						break;
				}
			}
		}

		void DispatchGenericArguments(IGenericInstance genericInstance, string referencingEntityName)
		{
			for (int i = 0; i < genericInstance.GenericArguments.Count; i++)
			{
				genericInstance.GenericArguments[i] = VisitType(genericInstance.GenericArguments[i], referencingEntityName);
			}
		}

		void DispatchInterfaces(TypeDefinition type, string referencingEntityName)
		{
			foreach (InterfaceImplementation? iface in type.Interfaces)
			{
				iface.InterfaceType = VisitType(iface.InterfaceType, referencingEntityName);
			}
		}

		void DispatchEvents(TypeDefinition type, string referencingEntityName)
		{
			foreach (EventDefinition? evt in type.Events)
			{
				evt.EventType = VisitType(evt.EventType, referencingEntityName);
				DispatchAttributes(evt, referencingEntityName);
			}
		}

		void DispatchProperties(TypeDefinition type, string referencingEntityName)
		{
			foreach (PropertyDefinition? property in type.Properties)
			{
				property.PropertyType = VisitType(property.PropertyType, referencingEntityName);
				DispatchAttributes(property, referencingEntityName);
			}
		}

		void DispatchFields(TypeDefinition type, string referencingEntityName)
		{
			foreach (FieldDefinition? field in type.Fields)
			{
				field.FieldType = VisitType(field.FieldType, referencingEntityName);
				DispatchAttributes(field, referencingEntityName);
			}
		}

		void DispatchAttributes(ICustomAttributeProvider provider, string referencingEntityName)
		{
			if (!provider.HasCustomAttributes)
			{
				return;
			}

			foreach (CustomAttribute? attribute in provider.CustomAttributes)
			{
				if (importMethodReference(attribute.Constructor) is { } reference)
				{
					attribute.Constructor = reference;
				}
				else
				{
					VisitMethod(attribute.Constructor, referencingEntityName);
				}

				for (int i = 0; i < attribute.ConstructorArguments.Count; ++i)
				{
					CustomAttributeArgument argument = attribute.ConstructorArguments[i];
					attribute.ConstructorArguments[i] = new CustomAttributeArgument(VisitType(argument.Type, referencingEntityName), argument.Value);
				}

				for (int i = 0; i < attribute.Properties.Count; ++i)
				{
					CustomAttributeNamedArgument namedArgument = attribute.Properties[i];
					attribute.Properties[i] = new CustomAttributeNamedArgument(namedArgument.Name, new CustomAttributeArgument(VisitType(namedArgument.Argument.Type, referencingEntityName), namedArgument.Argument.Value));
				}

				for (int i = 0; i < attribute.Fields.Count; ++i)
				{
					CustomAttributeNamedArgument namedArgument = attribute.Fields[i];
					attribute.Fields[i] = new CustomAttributeNamedArgument(namedArgument.Name, new CustomAttributeArgument(VisitType(namedArgument.Argument.Type, referencingEntityName), namedArgument.Argument.Value));
				}
			}
		}

		void VisitMethod(MethodReference? method, string referencingEntityName)
		{
			if (method == null)
			{
				return;
			}

			if (method is GenericInstanceMethod genericInstance)
			{
				DispatchGenericArguments(genericInstance, referencingEntityName);
			}

			method.ReturnType = VisitType(method.ReturnType, referencingEntityName);

			foreach (ParameterDefinition? parameter in method.Parameters)
			{
				parameter.ParameterType = VisitType(parameter.ParameterType, referencingEntityName);
			}

			// a definition keeps its declaring type: only references are redirected
			if (method is not MethodSpecification and not MethodDefinition)
			{
				method.DeclaringType = VisitType(method.DeclaringType, referencingEntityName);
			}
		}

		void VisitField(FieldReference? field, string referencingEntityName)
		{
			if (field == null)
			{
				return;
			}

			field.FieldType = VisitType(field.FieldType, referencingEntityName);

			if (field is not FieldDefinition)
			{
				field.DeclaringType = VisitType(field.DeclaringType, referencingEntityName);
			}
		}

		TypeReference? VisitType(TypeReference? type, string referencingEntityName)
		{
			if (type == null)
			{
				return type;
			}

			if (type.GetElementType().IsGenericParameter)
			{
				return type;
			}

			if (type is GenericInstanceType genericInstance)
			{
				DispatchGenericArguments(genericInstance, referencingEntityName);
			}

			return FixupType(type);
		}

		TypeReference FixupType(TypeReference type)
		{
			if (type.Scope == module && redirectedNamespaces.Contains(baseDeclaringType(type).Namespace))
			{
				if (type.IsNested)
				{
					// a type nested in a redirected type (the closures, iterators and helper types of an API copy
					// the mod embeds) stays in the module: the real assembly's nested types are different objects,
					// and a reference to the outer type in its place corrupts the definitions inside it
					return type;
				}

				if (patchingAssembly.GetType(type.FullName) is { } originalType)
				{
					return module.ImportReference(originalType);
				}
			}

			return type;
		}

		//List<TypeDefinition> typesToRemove = new();
		foreach (TypeDefinition type in module.GetTypes())
		{
			if (patchingAssembly.GetType(type.FullName) is null)
			{
				Dispatch(type);
			}
			/*
			else
			{
				typesToRemove.Add(type);
			}
			*/
		}
		/*
		foreach (TypeDefinition type in typesToRemove)
		{
			IMetadataScope scope = type.Scope;
			ModuleDefinition typeModule = type.Module;
			module.Types.Remove(type);
			type.Scope = scope;
			type.GetType().GetField("module", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(type, typeModule);
		}
		*/
	}

	public static bool PreventHarmonyInteropFixLoad(Assembly? __0) => __0 == null;

	public static void Patch(IEnumerable<string>? extraNamespaces = null)
	{
		Harmony harmony = new("org.bepinex.plugins.APIManager");
		harmony.Patch(AccessTools.DeclaredMethod(typeof(PluginInfo), nameof(PluginInfo.ToString)), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Patcher), nameof(GrabPluginInfo))));
		harmony.Patch(AccessTools.DeclaredMethod(typeof(Assembly), nameof(Assembly.LoadFile), new[] { typeof(string) }), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Patcher), nameof(InterceptAssemblyLoadFile))));
		harmony.Patch(AccessTools.DeclaredMethod(typeof(Assembly), nameof(Assembly.LoadFile), new[] { typeof(string) }), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Patcher), nameof(CheckAssemblyLoadFile))));
		harmony.Patch(AccessTools.DeclaredMethod(typeof(Assembly), nameof(Assembly.LoadFile), new[] { typeof(string) }), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Patcher), nameof(ReplaceAssemblyLoadWithCache))));
		new PatchClassProcessor(harmony, typeof(AssemblyLoadInterceptor), true).Patch();
		if (typeof(AssemblyPatcher).Assembly.GetType("BepInEx.Preloader.RuntimeFixes.HarmonyInteropFix") is { } interopFix)
		{
			harmony.Patch(AccessTools.DeclaredMethod(interopFix, "OnAssemblyLoad"), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Patcher), nameof(PreventHarmonyInteropFixLoad))));
		}

		IEnumerable<TypeInfo> types;
		try
		{
			types = patchingAssembly.DefinedTypes.ToList();
		}
		catch (ReflectionTypeLoadException e)
		{
			types = e.Types.Where(t => t != null).Select(t => t.GetTypeInfo());
		}
		BaseUnityPlugin plugin = (BaseUnityPlugin)BepInEx.Bootstrap.Chainloader.ManagerObject.GetComponent(types.First(t => t.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(t)));
		redirectedNamespaces = new HashSet<string>(extraNamespaces ?? Array.Empty<string>())
		{
			plugin.GetType().Namespace!,
		};
		modGUID = plugin.Info.Metadata.GUID;
	}
}
