# Character selection without an active world

On Valheim 1.0.16, `PlayerProfile.GetStat` may select achievement difficulty through
`Achievements.GetCurrentAchievementDifficultyIndex`. Character selection has no
active world yet, so this can throw a NullReferenceException and interrupt portrait
setup. Affected rows show prefab sample values and blank portraits.

Both Auga character-stat displays now read `m_playerStats[PlayerProfile.c_RawStats]`.
These are the saved lifetime counters, incremented independently of achievement
eligibility or world difficulty. No achievement methods are patched and no save
data is changed.

Portrait capture also restores the previous active render texture in a `finally`
block, disposes the temporary texture, and creates the output directory before
saving a portrait.

Validation on Valheim 1.0.16: an isolated character-menu run reproduced the old
worldless `GetStat` exception with achievements enabled, then verified displayed
names, lifetime statistics and portraits for all test profiles. Fifteen checks
passed without game errors. The test helper is not part of the release plugin.
