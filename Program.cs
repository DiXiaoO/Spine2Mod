using System;
using System.IO;
using Spine;

namespace Spine2Mod
{
    class Program
    {
        static void Main(string[] args)
        {
            // Check that at least 2 required arguments are present

            if (args.Length < 4)
            {
                Console.WriteLine("[Spine2Mod]\n" +
                        "-h - the hash of the card element you want to replace (Required argument!)\n" +
                        "-n - mod name (if it matches the atlas file and the json file of the spine file, then you can not use -a and -s) (Required argument!)\n" +
                        "-a --atlas - path to .atlas file\n" +
                        "-s --spine - path to spine file in .json or .skel format\n" +
                        "-f --fps - animation fps\n" +
                        "-an --animation - the name of the animation you want to run\n" +
                        "--resize - resize the model\n" +
                        "--widthShift - shift the model in width\n" +
                        "--heightShift - move the model up\n");
                return;
            }

            string name = null;
            string hash = null;
            string atlasFilePath = null;
            string skeletonFilePath = null;
            int fps = 60;
            string animationName = null;
            float resize = 1.0f;
            float widthShift = 0.0f;
            float heightShift = 0.0f;

            for (int i = 0; i < args.Length; i += 2)
            {
                string arg = args[i];
                string value = args[i + 1];

                switch (arg)
                {
                    case "-n":
                        name = value;
                        break;
                    case "-h":
                        hash = value;
                        break;
                    case "-a":
                    case "--atlas":
                        atlasFilePath = value;
                        break;
                    case "-s":
                    case "--spine":
                        skeletonFilePath = value;
                        break;
                    case "-f":
                    case "--fps":
                        int.TryParse(value, out fps);
                        break;
                    case "-an":
                    case "--animation":
                        animationName = value;
                        break;
                    case "--resize":
                        float.TryParse(value, out resize);
                        break;
                    case "--widthShift":
                        float.TryParse(value, out widthShift);
                        break;
                    case "--heightShift":
                        float.TryParse(value, out heightShift);
                        break;
                    default:
                        Console.WriteLine($"Unknown argument: {arg}");
                        break;
                }
            }

            if (atlasFilePath == null)
            {
                atlasFilePath = $"{name}.atlas";
            }

            if (skeletonFilePath == null)
            {
                skeletonFilePath = $"{name}.json";
            }

            if (name == null || hash == null)
            {
                Console.WriteLine("Required -n and -h arguments not specified");
                return;
            }

            // Проверяем расширение файлов
            if (!skeletonFilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
                !skeletonFilePath.EndsWith(".skel", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Error: Invalid file extension skeletonFilePath. Expected .json or .skel");
                return;
            }

            if (!atlasFilePath.EndsWith(".atlas", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Error: Invalid atlasFilePath file extension. Expected .atlas");
                return;
            }

            TextureLoader textureLoader = new DemoLoader();
            Atlas atlas = new Atlas(atlasFilePath, textureLoader);
            AtlasAttachmentLoader attachmentLoader = new AtlasAttachmentLoader(atlas);
            SkeletonData skeletonData;

            if (skeletonFilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                SkeletonJson skeletonJson = new SkeletonJson(attachmentLoader);
                skeletonData = skeletonJson.ReadSkeletonData(skeletonFilePath);
            }
            else
            {
                SkeletonBinary skeletonBinary = new SkeletonBinary(attachmentLoader);
                skeletonData = skeletonBinary.ReadSkeletonData(skeletonFilePath);
            }

            Console.WriteLine($"Spine version: {skeletonData.Version}");

            Skeleton skeleton = new Skeleton(skeletonData);

            AnimationStateData animationStateData = new AnimationStateData(skeletonData);
            AnimationState animationState = new AnimationState(animationStateData);
            Animation animation;

            if (animationName == null)
            {
                animation = skeletonData.Animations.Items[0];
            }
            else
            {
                animation = skeletonData.FindAnimation(animationName);
            }

            float animationDuration = animation.Duration;

            // Setting the start state of the animation
            animationState.SetAnimation(0, animation, true);

            // Animation playback (assuming frames are updated in a loop))
            float deltaTime = 1.0f / fps;
            int frames = (int)(animationDuration / deltaTime)+1;
            Console.WriteLine($"Number of frames to render: {frames}");
            float num = 0;

            byte[] r8UnormValues = { 0xFF, 0xFF, 0xFF, 0xFF };

            string modPath = CreateMod(name, fps, frames, hash);

            while (true)
            {
                animationState.Update(deltaTime);
                animationState.Apply(skeleton);
                skeleton.UpdateWorldTransform();


                // ib
                if (num == 0)
                {
                    string ibPath = $"{modPath}/ib.ib";

                    int countIB = 0;

                    using (FileStream fsIB = new FileStream(ibPath, FileMode.Create))
                    using (BinaryWriter writerIB = new BinaryWriter(fsIB))
                    {
                        foreach (Slot slot in skeleton.Slots)
                        {
                            if (slot.Attachment is MeshAttachment meshAttachment)
                            {
                                var triangles = meshAttachment.Triangles;
                                var worldVertices = new float[meshAttachment.WorldVerticesLength];

                                for (int i = 0; i < triangles.Length; i += 3)
                                {
                                    int vertexIndex1 = triangles[i];
                                    int vertexIndex2 = triangles[i + 1];
                                    int vertexIndex3 = triangles[i + 2];

                                    vertexIndex1 += countIB;
                                    vertexIndex2 += countIB;
                                    vertexIndex3 += countIB;

                                    writerIB.Write(BitConverter.GetBytes(vertexIndex1)[0]);
                                    writerIB.Write(BitConverter.GetBytes(vertexIndex1)[1]);
                                    writerIB.Write(BitConverter.GetBytes(vertexIndex2)[0]);
                                    writerIB.Write(BitConverter.GetBytes(vertexIndex2)[1]);
                                    writerIB.Write(BitConverter.GetBytes(vertexIndex3)[0]);
                                    writerIB.Write(BitConverter.GetBytes(vertexIndex3)[1]);
                                }

                                countIB += (int)(worldVertices.Length / 2);
                            }
                            else if (slot.Attachment is RegionAttachment)
                            {
                                int vertexIndex1 = 0;
                                int vertexIndex2 = 1;
                                int vertexIndex3 = 2;
                                int vertexIndex4 = 3;

                                vertexIndex1 += countIB;
                                vertexIndex2 += countIB;
                                vertexIndex3 += countIB;
                                vertexIndex4 += countIB;


                                writerIB.Write(BitConverter.GetBytes(vertexIndex1)[0]);
                                writerIB.Write(BitConverter.GetBytes(vertexIndex1)[1]);
                                writerIB.Write(BitConverter.GetBytes(vertexIndex2)[0]);
                                writerIB.Write(BitConverter.GetBytes(vertexIndex2)[1]);
                                writerIB.Write(BitConverter.GetBytes(vertexIndex3)[0]);
                                writerIB.Write(BitConverter.GetBytes(vertexIndex3)[1]);

                                writerIB.Write(BitConverter.GetBytes(vertexIndex3)[0]);
                                writerIB.Write(BitConverter.GetBytes(vertexIndex3)[1]);
                                writerIB.Write(BitConverter.GetBytes(vertexIndex4)[0]);
                                writerIB.Write(BitConverter.GetBytes(vertexIndex4)[1]);
                                writerIB.Write(BitConverter.GetBytes(vertexIndex1)[0]);
                                writerIB.Write(BitConverter.GetBytes(vertexIndex1)[1]);

                                countIB += (int)4;
                            }
                            /*else if (slot.Attachment is ClippingAttachment clippingAttachment) 
                            {
                                //I have not yet figured out how to implement such a mask in 3DMigoto
                            }*/
                            /*else if (slot.Attachment is PathAttachment pathAttachment) 
                            {
                                //I have not yet figured out how to implement this in 3DMigoto
                            }*/
                            else
                            {
                                if (slot.Attachment != null) Console.WriteLine($"Unrealized type: {slot.Attachment.GetType()}");
                            }
                        }
                    }
                }


                // vb
                string bufPath = $"{modPath}/frames/vb_{num}.buf";

                using (FileStream fs = new FileStream(bufPath, FileMode.Create))
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    foreach (Slot slot in skeleton.Slots)
                    {

                        if (slot.Attachment is MeshAttachment meshAttachment)
                        {
                            var triangles = meshAttachment.Triangles;
                            var uv = meshAttachment.UVs;
                            var worldVertices = new float[meshAttachment.WorldVerticesLength];

                            meshAttachment.ComputeWorldVertices(slot, 0, meshAttachment.WorldVerticesLength, worldVertices, 0);
                            for (int i = 0; i < worldVertices.Length; i += 2)
                            {
                                writer.Write(BitConverter.GetBytes(worldVertices[i] * resize + widthShift));
                                writer.Write(BitConverter.GetBytes(worldVertices[i + 1] * resize + heightShift));
                                writer.Write(BitConverter.GetBytes(0));

                                writer.Write(r8UnormValues);

                                writer.Write(BitConverter.GetBytes(uv[i]));
                                writer.Write(BitConverter.GetBytes(1 - uv[i + 1]));
                            }

                        }
                        else if (slot.Attachment is RegionAttachment regionAttachment)
                        {
                            var uv = regionAttachment.UVs;
                            var vertices = new float[8];
                            regionAttachment.ComputeWorldVertices(slot, vertices, 0);

                            for (int i = 0; i < 8; i += 2)
                            {
                                writer.Write(BitConverter.GetBytes(vertices[i] * resize + widthShift));
                                writer.Write(BitConverter.GetBytes(vertices[i + 1] * resize + heightShift));
                                writer.Write(BitConverter.GetBytes(0));

                                writer.Write(r8UnormValues);

                                writer.Write(BitConverter.GetBytes(uv[i]));
                                writer.Write(BitConverter.GetBytes(1 - uv[i + 1]));
                            }
                        }
                        /*else if (slot.Attachment is ClippingAttachment clippingAttachment)
                        {
                            //I have not yet figured out how to implement such a mask in 3DMigoto
                        }*/
                        /*else if (slot.Attachment is PathAttachment pathAttachment)
                        {
                            //I have not yet figured out how to implement this in 3DMigoto
                        }*/
                    }
                }


                if (num > frames)
                {
                    return;
                }

                num += 1;
            }
        }


        class DemoLoader : TextureLoader
        {
            public void Load(AtlasPage page, string path)
            {
                return;
            }

            public void Unload(object texture)
            {
                return;
            }
        }

        static string CreateMod(string modName,int fps, int frames, string hash)
        {
            string folderPath = $"Mod{modName}";
            string folderPathFrames = $"Mod{modName}/frames";

            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    Console.WriteLine("Folder created successfully!");
                }
                else
                {
                    Console.WriteLine("The folder already exists.");
                }

                if (!Directory.Exists(folderPathFrames))
                {
                    Directory.CreateDirectory(folderPathFrames);
                    Console.WriteLine("Folder created successfully!");
                }
                else
                {
                    Console.WriteLine("The folder already exists.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating folder: {ex.Message}");
            }

            string filePath = $"Mod{modName}/Mod{modName}.ini";

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("[Constants]\nglobal $frame = 0\n\n[Present]");
                    writer.WriteLine($"$frame = (time // {(float)(1.0f / (float)fps)}) % {frames}\n\n[TextureOverride{modName}]\nhash = {hash}\nps-t0 = null\nps-t1 = null\nps-t2 = ResourceT2\nps-t3 = null\nib = ResourceIB\nrun = CommandListAnimation\nrun = CustomShaderFix\n\n[CustomShaderFix]\nhandling = skip\ndrawindexed = auto\n\n[CommandListAnimation]\nif $frame == 0\n    vb0 = ResourceFrame0");
                    for (int i = 1; i < frames + 1; i += 1)
                    {
                        writer.WriteLine($"elif $frame == {i}");
                        writer.WriteLine($"    vb0 = ResourceFrame{i}");
                    }
                    writer.WriteLine("endif\n");
                    writer.WriteLine("[ResourceIB]\ntype = Buffer\nformat = R16_UINT\nfilename = ib.ib\n\n[ResourceT2]\nfilename = texture.dds\n");
                    for (int i = 0; i < frames + 1; i += 1)
                    {
                        writer.WriteLine($"[ResourceFrame{i}]\ntype = Buffer\nstride = 24\nfilename = frames/vb_{i}.buf");
                    }
                }

                Console.WriteLine("The data was successfully written to the file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to file: {ex.Message}");
            }

            return folderPath;
        }
    }
}
