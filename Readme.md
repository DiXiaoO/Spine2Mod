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
after creating the mod, you need to make a texture.dds file and put it in the mod folder.  
texture.dds is the image used for the spine animation. The image must be flipped on the y-axis and must be converted to .dds sRGB format. (If you have problems converting, you can try to make the image square. For example, 1024x1024 and so on)  

the resulting mod replaces Yae Miko card.   

[hero spine](https://github.com/EsotericSoftware/spine-runtimes/tree/4.1/examples/hero/export)

[The program uses spine-csharp.dll](https://github.com/EsotericSoftware/spine-runtimes/)