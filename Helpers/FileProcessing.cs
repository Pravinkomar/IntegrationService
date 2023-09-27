using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IntegrationService.Helpers
{
    class FileProcessing
    {

        public static void moveFile(string file, string sourceDir, string targetDir)
        {
            try
            {
                if (File.Exists(targetDir + file))
                {
                    File.Delete(targetDir + file);
                }
                File.Move(sourceDir + file, targetDir + file);

            }
            catch (IOException e)
            {
            }
        }
        public static void copyFile(string file, string sourceDir, string targetDir)
        {
            try
            {
                if (File.Exists(targetDir + file))
                {
                    File.Delete(targetDir + file);
                }
                File.Copy(sourceDir + file, targetDir + file);

            }
            catch (IOException e)
            {
            }
        }
        public static void deleteFile(string file, string sourceDir)
        {
            try
            {
                if (File.Exists(sourceDir + file))
                {
                    File.Delete(sourceDir + file);
                }
            }
            catch (IOException e)
            {
            }
        }
    }
}
