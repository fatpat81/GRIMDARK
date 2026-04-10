using System.Drawing;
using System.IO;

class Program {
    static void Main() {
        Image img = Image.FromFile("wh40k_logo.png");
        Bitmap b2 = new Bitmap(img, new Size(256, 256));
        using (FileStream fs = new FileStream("icon.ico", FileMode.Create)) {
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write((short)0);
            bw.Write((short)1);
            bw.Write((short)1);
            
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((short)1);
            bw.Write((short)32);
            
            MemoryStream pngStream = new MemoryStream();
            b2.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
            byte[] pngData = pngStream.ToArray();
            
            bw.Write((int)pngData.Length);
            bw.Write((int)22);
            bw.Write(pngData);
        }
    }
}
