using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.IO;
using System;
using System.Collections.Generic;
using System.IO;

namespace VictoremLibrary
{
    /// <summary>
    /// Помогает использовать директиву #include в HLSL файлах
    /// </summary>
    public class HLSLFileIncludeHandler : CallbackBase, Include
    {
        public readonly Stack<string> CurrentDirectory;
        public readonly List<string> IncludeDirectories;

        /// <summary>
        /// Конструктор класса. Класс помогает использовать директиву #include в HLSL файлах
        /// </summary>
        /// <param name="initialDirectory">Папка с шейдерами</param>
        public HLSLFileIncludeHandler(string initialDirectory)
        {
            IncludeDirectories = new List<string>();
            CurrentDirectory = new Stack<string>();
            CurrentDirectory.Push(initialDirectory);
        }

        #region Include Members

        public Stream Open(IncludeType type, string fileName, Stream parentStream)
        {
            var currentDirectory = CurrentDirectory.Peek();
            if (currentDirectory == null)
#if NETFX_CORE
                currentDirectory = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
#else
                currentDirectory = Environment.CurrentDirectory;
#endif

            var filePath = fileName;

            if (!Path.IsPathRooted(filePath))
            {
                var directoryToSearch = new List<string> { currentDirectory };
                directoryToSearch.AddRange(IncludeDirectories);
                foreach (var dirPath in directoryToSearch)
                {
                    var selectedFile = Path.Combine(dirPath, fileName);
                    if (NativeFile.Exists(selectedFile))
                    {
                        filePath = selectedFile;
                        break;
                    }
                }
            }

            if (filePath == null || !NativeFile.Exists(filePath))
            {
                throw new FileNotFoundException(String.Format("Unable to find file [{0}]", filePath ?? fileName));
            }

            NativeFileStream fs = new NativeFileStream(filePath, NativeFileMode.Open, NativeFileAccess.Read);
            CurrentDirectory.Push(Path.GetDirectoryName(filePath));
            return fs;
        }

        public void Close(Stream stream)
        {
            stream.Dispose();
            CurrentDirectory.Pop();
        }

        #endregion
    }
}
