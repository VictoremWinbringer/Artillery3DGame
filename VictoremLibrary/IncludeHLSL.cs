using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.IO;
using System.IO;

namespace VictoremLibrary
{
    public class IncludeHLSL : CallbackBase, Include
    {
        private string includeDirectory;

        IncludeHLSL(string directory)
        {
            includeDirectory = directory;
        }

        public void Close(Stream stream) =>
            stream.Dispose();

        public Stream Open(IncludeType type, string fileName, Stream parentStream) =>
            new NativeFileStream(includeDirectory + fileName, NativeFileMode.Open, NativeFileAccess.Read);
    }
}
