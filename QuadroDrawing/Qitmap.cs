using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace QuadroDrawing
{
    /// <summary> Object like Bitmap, but weird encryption and decryption </summary>
    public class Qitmap
    {
        byte[] bytes;

        private bool is_powerOf2(int n)
        {
            int b = 1;
            int c = 0;
            while (b != 0)
            {
                c += (b & n) != 0 ? 1 : 0;
                b <<= 1;
            }
            return c == 1;
        }

        public Qitmap(Bitmap bmp)
        {
            if (bmp.Width != bmp.Height) throw new Exception("square only");

            if (!is_powerOf2(bmp.Width)) throw new Exception("power of 2 only");

            List<byte> bs = new List<byte>();

            bs.AddRange(BitConverter.GetBytes(bmp.Width));

            chunk_handler(bs, bmp, Color.FromArgb(0, 0, 0, 0), 0, 0, bmp.Width);

            bytes = bs.ToArray();

            bs.Clear();
        }

        private Color color_handler(Bitmap bmp, int x, int y, int s)
        {
            if (s == 1) return bmp.GetPixel(x, y);

            int s0 = s / 2;
            Color c0 = color_handler(bmp, x, y, s0);
            Color c1 = color_handler(bmp, x + s0, y, s0);
            Color c2 = color_handler(bmp, x, y + s0, s0);
            Color c3 = color_handler(bmp, x + s0, y + s0, s0);

            int r = c0.R + c1.R + c2.R + c3.R;
            int g = c0.G + c1.G + c2.G + c3.G;
            int b = c0.B + c1.B + c2.B + c3.B;
            int a = c0.A + c1.A + c2.A + c3.A;

            return Color.FromArgb(a / 4, r / 4, g / 4, b / 4);
        }

        private void chunk_handler(List<byte> bs, Bitmap bmp, Color parentColor, int x, int y, int s)
        {
            int s0 = s / 2;
            byte da, dr, dg, db;
            Color c;

            if (s == 1)
            {
                c = bmp.GetPixel(x, y);

                bs.Add(255);

                da = (byte)(parentColor.A - c.A);
                dr = (byte)(parentColor.R - c.R);
                dg = (byte)(parentColor.G - c.G);
                db = (byte)(parentColor.B - c.B);

                bs.Add(da);
                bs.Add(dr);
                bs.Add(dg);
                bs.Add(db);

                return;
            }

            Color c0 = color_handler(bmp, x, y, s0);
            Color c1 = color_handler(bmp, x + s0, y, s0);
            Color c2 = color_handler(bmp, x, y + s0, s0);
            Color c3 = color_handler(bmp, x + s0, y + s0, s0);

            byte h0 = (byte)(!c0.Equals(parentColor) ? 0x1 : 0);
            byte h1 = (byte)(!c1.Equals(parentColor) ? 0x2 : 0);
            byte h2 = (byte)(!c2.Equals(parentColor) ? 0x4 : 0);
            byte h3 = (byte)(!c3.Equals(parentColor) ? 0x8 : 0);

            //         0 1 2 3
            // 0 0 0 0 1 1 1 1
            bs.Add((byte)(h0 | h1 | h2 | h3)); // no sub chunks

            int r = c0.R + c1.R + c2.R + c3.R;
            int g = c0.G + c1.G + c2.G + c3.G;
            int b = c0.B + c1.B + c2.B + c3.B;
            int a = c0.A + c1.A + c2.A + c3.A;

            c = Color.FromArgb(a / 4, r / 4, g / 4, b / 4);

            da = (byte)(parentColor.A - c.A);
            dr = (byte)(parentColor.R - c.R);
            db = (byte)(parentColor.B - c.B);
            dg = (byte)(parentColor.G - c.G);

            bs.Add(da);
            bs.Add(dr);
            bs.Add(dg);
            bs.Add(db);

            if (h0 != 0) chunk_handler(bs, bmp, c, x, y, s0);
            if (h1 != 0) chunk_handler(bs, bmp, c, x + s0, y, s0);
            if (h2 != 0) chunk_handler(bs, bmp, c, x, y + s0, s0);
            if (h3 != 0) chunk_handler(bs, bmp, c, x + s0, y + s0, s0);
        }

        public Bitmap GetBitmap(int maxlvl)
        {
            int s = BitConverter.ToInt32(bytes, 0);

            Bitmap bmp = new Bitmap(s, s);

            Graphics gr = Graphics.FromImage(bmp);

            int i = 4;

            bytechunk_handler(gr, Color.FromArgb(0, 0, 0, 0), ref i, 0, maxlvl, 0, 0, s);

            return bmp;
        }

        private void bytechunk_handler(Graphics bmp, Color parentColor, ref int i, int lvl, int maxlvl, int x, int y, int s)
        {
            byte m = bytes[i++];

            byte da = bytes[i++];
            byte dr = bytes[i++];
            byte dg = bytes[i++];
            byte db = bytes[i++];

            Color c = Color.FromArgb((byte)(parentColor.A - da),
                                     (byte)(parentColor.R - dr),
                                     (byte)(parentColor.G - dg),
                                     (byte)(parentColor.B - db));

            SolidBrush br = new SolidBrush(c);

            if (s == 1)
            {
                bmp.FillRectangle(br, x, y, s, s);
            }
            else
            {
                int s0 = s / 2;

                if (lvl < maxlvl)
                {
                    lvl++;

                    if ((m & 0x1) != 0) bytechunk_handler(bmp, c, ref i, lvl, maxlvl, x, y, s0);
                    else bmp.FillRectangle(br, x, y, s0, s0);
                    if ((m & 0x2) != 0) bytechunk_handler(bmp, c, ref i, lvl, maxlvl, x + s0, y, s0);
                    else bmp.FillRectangle(br, x + s0, y, s0, s0);
                    if ((m & 0x4) != 0) bytechunk_handler(bmp, c, ref i, lvl, maxlvl, x, y + s0, s0);
                    else bmp.FillRectangle(br, x, y + s0, s0, s0);
                    if ((m & 0x8) != 0) bytechunk_handler(bmp, c, ref i, lvl, maxlvl, x + s0, y + s0, s0);
                    else bmp.FillRectangle(br, x + s0, y + s0, s0, s0);
                }
                else
                {
                    bmp.FillRectangle(br, x, y, s, s);
                }
            }
        }
    }
}
