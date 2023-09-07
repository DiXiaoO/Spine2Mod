# Spine2Mod
-h - the hash of the card element you want to replace (Required argument!)
-n - mod name (if it matches the atlas file and the json file of the spine file, then you can not use -a and -s) (Required argument!)
-a --atlas - path to .atlas file
-s --spine - path to spine file in .json or .skel format
-f --fps - animation fps
-an --animation - the name of the animation you want to run
--resize - resize the model
--widthShift - shift the model in width
--heightShift - move the model up

## Usage example
./Spine2Mod.exe -s hero-ess.json -a hero.atlas -an attack -f 24 -h c7d59e68 -n hero --resize 0.0116

[The program uses spine-csharp.dll](https://github.com/EsotericSoftware/spine-runtimes/)