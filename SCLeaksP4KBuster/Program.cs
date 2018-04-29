﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZstdNet;

namespace SCLeaksP4KBuster
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] buffer = new byte[16];
            FileStream fs = new FileStream(".\\Data.p4k", FileMode.Open, FileAccess.Read);
            byte[] headerIdentifier = { 0x50, 0x4B, 0x03, 0x14 };
            long currentChunk = 0;
            long chunkLength = (fs.Length / 16);
            int firstTick = System.Environment.TickCount;
            int lastTick = firstTick;
            long firstChunk = 0;
            double chunkEta = 0;

            if (File.Exists(".\\lastChunk.txt"))
            {
                using (var stream = File.Open(".\\lastChunk.txt", FileMode.Open))
                {
                    byte[] chunkFileData = new byte[stream.Length];
                    stream.Read(chunkFileData, 0, (int) stream.Length);
                    long.TryParse(Encoding.ASCII.GetString(chunkFileData), out currentChunk);
                    firstChunk = currentChunk;
                    fs.Seek(currentChunk * 16, SeekOrigin.Begin);
                }
            }

            do
            {
                //Read bytes from the file with offset currentChunk and save to buffer
                fs.Read(buffer, 0, 16);
                currentChunk++;

                //Create a header byte array and copy the first 4 bytes from the buffer
                byte[] headerIdBytes = new byte[headerIdentifier.Length];
                Array.Copy(buffer, 0, headerIdBytes, 0, 4);

                if (headerIdBytes.SequenceEqual(headerIdentifier))
                {
                    //Header identified, proceding to determine compression method
                    byte[] compressionMethodBytes = new byte[2];
                    Array.Copy(buffer, 8, compressionMethodBytes, 0, 2);
                    currentChunk = ProcessFile(fs, buffer, currentChunk, compressionMethodBytes);
                    using (FileStream fs3 = new FileStream("lastChunk.txt", FileMode.Create, FileAccess.Write))
                    {
                        fs3.Write(Encoding.ASCII.GetBytes(currentChunk.ToString()), 0, Encoding.ASCII.GetBytes(currentChunk.ToString()).Length);
                        
                        //calculate time remaining and print progress
                        double chunkPercent = (((double)currentChunk / chunkLength) * 100);
                        if ((System.Environment.TickCount - lastTick) > 1000)
                        {
                            chunkEta = ((double) chunkLength / ((currentChunk - firstChunk) / (System.Environment.TickCount - firstTick) * 1000));
                            lastTick = System.Environment.TickCount;
                        }
                        double timePassed = ((System.Environment.TickCount - firstTick)/1000);
                        double minuteEta = (chunkEta / 60) - (timePassed / 60);
                        double secondEta = (chunkEta % 60) - (timePassed % 60);
                        if (secondEta < 0)
                        {
                            secondEta = 60 + secondEta;
                            minuteEta--;
                        }
                        Console.Write("\r" + chunkPercent.ToString("0.000")
                            + "% (" + currentChunk + "/" + chunkLength + ") "
                            + minuteEta.ToString("0") + ":" + secondEta.ToString("00") + " Time Remaining ("
                            + (timePassed / 60).ToString("0") + ":" + (timePassed % 60).ToString("00") + " Passed)    "
                            );
                    }
                }

                
            }
            while (currentChunk <= fs.Length / 16);

            if (File.Exists(".\\lastChunk.txt"))
            {
                File.Delete(".\\lastChunk.txt");
            }
        }

        static long ProcessFile(FileStream fs, byte[] buffer, long currentChunk, byte[] compressionMethodBytes)
        {
            fs.Read(buffer, 0, 16);
            currentChunk++;

            //Get the file name size
            byte[] fileNameSizeBytes = new byte[2];
            Array.Copy(buffer, 0xa, fileNameSizeBytes, 0, 2);
            int fileNameSize = BitConverter.ToInt16(fileNameSizeBytes, 0);

            //Get the extra field size
            byte[] extrafieldSizeBytes = new byte[2];
            Array.Copy(buffer, 0xc, extrafieldSizeBytes, 0, 2);
            int extraFieldSize = BitConverter.ToInt16(extrafieldSizeBytes, 0);

            //Get the file name
            byte[] fileNameBytes = new byte[fileNameSize];
            Array.Copy(buffer, 0xe, fileNameBytes, 0, 2);

            int nameChunks = (int)Math.Ceiling((decimal)(fileNameSize - 2) / 16);
            int fileNameIndex = 2;
            int lastCharIndex = 0;

            while (fileNameIndex < fileNameSize)
            {
                fs.Read(buffer, 0, 16);
                currentChunk++;

                for (int i = 0; i < buffer.Length; i++)
                {
                    if (fileNameIndex >= fileNameSize)
                    {
                        lastCharIndex = i;
                        break;
                    }

                    fileNameBytes[fileNameIndex] = buffer[i];
                    fileNameIndex++;
                }
            }

            string fileName = Encoding.ASCII.GetString(fileNameBytes);

            long extraFieldChunks = (int)Math.Ceiling((decimal)(extraFieldSize) / 16) - 1;
            long extraFieldChunkStart = currentChunk;

            //Get the file size
            byte[] fileSizeBytes = new byte[4];
            lastCharIndex--;

            if (lastCharIndex < 3 && lastCharIndex >= 0)
            {
                Array.Copy(buffer, lastCharIndex + 13, fileSizeBytes, 0, 3 - lastCharIndex);
                fs.Read(buffer, 0, 16);
                currentChunk++;
                Array.Copy(buffer, 0, fileSizeBytes, 3 - lastCharIndex, lastCharIndex + 1);
            }
            else if (lastCharIndex < 15 && lastCharIndex >= 3)
            {
                fs.Read(buffer, 0, 16);
                currentChunk++;
                Array.Copy(buffer, lastCharIndex - 3, fileSizeBytes, 0, 4);
            }
            else
            {
                fs.Read(buffer, 0, 16);
                currentChunk++;
                extraFieldChunkStart = currentChunk;
                Array.Copy(buffer, 12, fileSizeBytes, 0, 4);
            }
            //Console.WriteLine(lastCharIndex);

            //Console.WriteLine(BitConverter.ToString(fileSizeBytes));
            //Console.ReadLine();
            int fileSize = BitConverter.ToInt32(fileSizeBytes, 0);

            //Get the actual file data
            long chunkOffset = currentChunk - extraFieldChunkStart;
            long garbageLength = extraFieldChunks - chunkOffset;

            byte[] garbage = new byte[garbageLength * 16];
            fs.Read(garbage, 0, (int)garbageLength * 16);
            currentChunk+=garbageLength;

            byte[] file = new byte[fileSize];
            fs.Read(file, 0, fileSize);
            fs.Read(garbage, 0, 16 - (fileSize % 16));
            currentChunk += (fileSize + (16 - (fileSize % 16)))/16;

            byte[] decompFile = null;
            //Console.WriteLine(fileName);
            //Console.WriteLine(fileSize + " Bytes");

            if (BitConverter.ToInt16(compressionMethodBytes, 0) == 0x64)
            {
                using (var decompressor = new Decompressor())
                {
                    try
                    {
                        decompFile = decompressor.Unwrap(file);                       
                        if (Path.GetDirectoryName(fileName) != "")
                            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                        
                        using (FileStream fs2 = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                        {
                            fs2.Write(decompFile, 0, decompFile.Length);
                        }                        
                    }
                    catch (ZstdException e)
                    {
                        Console.Write("\rSkipping the following file because it is broken. Size code: " + BitConverter.ToString(fileSizeBytes));
                        Console.Write("\nLast char index: " + lastCharIndex);
                        Console.Write("\nError: " + e.Message + "\n");
                        //Console.ReadLine();
                    }
                }
            }
            else
            {                
                decompFile = file;               
                if (Path.GetDirectoryName(fileName) != "")
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));                
                using (FileStream fs2 = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    fs2.Write(decompFile, 0, decompFile.Length);
                }    
            }
            
            return currentChunk;
        }
    }
}
