## GTFO Dimension Maps

A mod that allows you to see maps in alternate dimensions.  
By default, all dimensions get their own map.

### Rundown Dev Info

By creating files with the file name formated like `DimensionMaps_RundownID_Tier_ExpeditionIndex.json`, you're able to change which dimensions should be 'disconnected' on a per level basis.  
(See [Examples](#Examples) below!)

```json
{
    "EnableDimensionSeventeenToTwenty": false,
    "DimensionsToDisconnect": [
        0,
        2
    ]
}
```
#### Format

* `EnableDimensionSeventeenToTwenty` (bool)
  * Enables rendering of maps in dimensions 17 to 20 (snatcher dimensions)  
  * this is usually not needed unless you're using up all other 16 dimensions for some reason and are in desperate need for even more ... lol
* `DimensionsToDisconnect` (List&lt;uint&gt;)
  * Any dimension index entries in this list will have their maps 'disconnected' in game.
  * In the example above, dimension 0 (Reality, aka the main dimension) and dimension 2 will have their maps inaccessible.

#### Examples

Format: `DimensionMaps_RundownID_Tier_ExpeditionIndex.json`  
Examples:
* `DimensionMaps_41_A_0.json`
  * Rundown with ID 41 (= Rundown 6 in Vanilla)
  * A tier expedition
  * with index 0 => `R6A1`
* `DimensionMaps_1_TierC_2.json`
  * Rundown with ID 1 (usually used for modded rundowns)
  * C tier expedition
  * with index 2

`RundownID`: has to be an integer  
for `Tier` either single characters (`A`) or the full enum name (`TierA`) can be used.  
`ExpeditionIndex`: has to be an integer

#### Extra

By creating a file called `DimensionMaps_GlobalFallback.json`, you're able to define a *global fallback* for whenever a level does not have its own config.  
Using this fallback you're able to bring back the vanilla behaviour by 'disconnecting' all dimensions except for dimension 0 (=> Reality),  
or a little more deviously disable maps for any and all dimensions for every level.  
(Don't do the latter, or you're evil tho! >:c)

If a level *does have* its own config, **this global fallback is ignored** and the level specific one used instead.