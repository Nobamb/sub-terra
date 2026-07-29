using System.IO;

namespace SubTerra.App.Save
{
    public interface ISaveFileSystem
    {
        bool FileExists(string path);
        void CreateDirectory(string path);
        void WriteAllText(string path, string contents);
        string ReadAllText(string path);
        void DeleteFile(string path);
        void MoveFile(string sourcePath, string destinationPath);
    }

    public sealed class PhysicalSaveFileSystem : ISaveFileSystem
    {
        public bool FileExists(string path) => File.Exists(path);
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);
        public void WriteAllText(string path, string contents) =>
            File.WriteAllText(path, contents);
        public string ReadAllText(string path) => File.ReadAllText(path);
        public void DeleteFile(string path) => File.Delete(path);
        public void MoveFile(string sourcePath, string destinationPath) =>
            File.Move(sourcePath, destinationPath);
    }
}
