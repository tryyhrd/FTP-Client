namespace Common
{
    public class FileInfoFTP
    {
        public byte[] Data { get; set; }
        public string Name { get; set; }
        public FileInfoFTP(byte[] data, string name)
        {
            Data = data;
            Name = name;
        }
    }
}
