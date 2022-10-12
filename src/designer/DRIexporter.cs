using System.Numerics;
using System.Text;

namespace Designer {

    public class DriExporter {
        public List<DrawInstruction> DrawInstructions;
        private UInt64 index = 0;
        private UInt64 dotIndex = 0;
        private Int64 lastX = 0;
        private Int64 lastY = 0;

        public DriExporter() {
            DrawInstructions = new List<DrawInstruction>();
        }

        public void Export(String path) {
            //start new file
            // string path = "drawings/" + exportfilename + ".dri";
            // string path = exportfilename + ".dri";
            DrawInstruction dtemp = new DrawInstruction();
            byte[] bytes = FormatDrawInstruction(dtemp);
            Int32 version = 2;
            Int64 start = 60;
            byte size = (byte)bytes.Length;

            using (FileStream fileStream = new FileStream(path, FileMode.Create)) {
                // Write the data to the file, byte by byte.
                fileStream.Seek(0, SeekOrigin.Begin);
                fileStream.Write(Encoding.ASCII.GetBytes("DRI::DrawInstruction"));
                fileStream.Write(BitConverter.GetBytes(version));
                fileStream.Seek(32, SeekOrigin.Begin);
                fileStream.Write(BitConverter.GetBytes(start));
                fileStream.Write(BitConverter.GetBytes(size)); //lenghth of one instruction
            }
            //

            Int64 instructioncount = 0;
            index = 0;
            dotIndex = 0;
            lastX = 0;
            lastY = 0;
            foreach (Line l in Data.lines) {

                if (l.type == lineType.Quadratic3DBezier) {
                    plotBezier3d(
                        (Int64)l.points[0].X, (Int64)l.points[0].Y, (Int64)l.points[0].Z,
                        (Int64)l.points[1].X, (Int64)l.points[1].Y, (Int64)l.points[1].Z,
                        (Int64)l.points[2].X, (Int64)l.points[2].Y, (Int64)l.points[2].Z
                    );
                    // DrawBezier3DSegment(
                    //     (Int64)l.points[0].X, (Int64)l.points[0].Y, (Int64)l.points[0].Z,
                    //     (Int64)l.points[1].X, (Int64)l.points[1].Y, (Int64)l.points[1].Z,
                    //     (Int64)l.points[2].X, (Int64)l.points[2].Y, (Int64)l.points[2].Z
                    // );
                }

                if (l.type == lineType.Straight3D) {
                    DrawLine3D(
                        (Int64)l.points[0].X, (Int64)l.points[0].Y, (Int64)l.points[0].Z,
                        (Int64)l.points[1].X, (Int64)l.points[1].Y, (Int64)l.points[1].Z
                    );
                }
                DrawInstructions.Clear();

                if (l.type == lineType.Straight) {
                    for (int i = 0; i < l.lineData.Length - 1; i++) {
                        addLine((Int64)l.lineData[i].X, (Int64)l.lineData[i].Y, (Int64)l.lineData[i + 1].X, (Int64)l.lineData[i + 1].Y);
                    }
                }

                if (l.type == lineType.QuadraticBezier) {
                    addBezier((Int64)l.lineData[0].X, (Int64)l.lineData[0].Y, (Int64)l.lineData[1].X, (Int64)l.lineData[1].Y, (Int64)l.lineData[2].X, (Int64)l.lineData[2].Y);
                }

                // if (l.type == lineType.Quadratic3DBezier) {
                //     addBezier3DSegment(
                //         (Int64)l.points[0].X, (Int64)l.points[0].Y, (Int64)l.points[0].Z, 
                //         (Int64)l.points[1].X, (Int64)l.points[1].Y, (Int64)l.points[1].Z,
                //         (Int64)l.points[2].X, (Int64)l.points[2].Y, (Int64)l.points[2].Z
                //     );
                // }
                //save all instructions in the list

                foreach (DrawInstruction d in DrawInstructions) {
                    // Console.WriteLine($"now debugging line: {d.index}");
                    // draw(d);

                    using (FileStream fileStream = new FileStream(path, FileMode.Open)) {
                        fileStream.Seek(start + instructioncount * size, SeekOrigin.Begin);
                        fileStream.Write(FormatDrawInstruction(d));
                    }

                    instructioncount++;
                }

            }
            Console.Write("written:");
            Console.Write(instructioncount);
            Console.WriteLine(" drawinstructions");

            Data.DebugConsole.Add("Written: " + instructioncount.ToString() + " drawinstructions");

            using (FileStream fileStream = new FileStream(path, FileMode.Open)) {
                fileStream.Seek(24, SeekOrigin.Begin);
                fileStream.Write(BitConverter.GetBytes(instructioncount));
                fileStream.Seek(start + instructioncount * size, SeekOrigin.Begin);
                fileStream.WriteByte(0);
            }
        }


        byte[] FormatDrawInstruction(DrawInstruction d) {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(d.index));
            bytes.Add(0);
            bytes.Add((byte)d.type);
            bytes.Add(0);
            bytes.Add((byte)d.dir_x);
            bytes.Add(0);
            bytes.Add((byte)d.dir_y);
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.x_start));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.y_start));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.x_end));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.y_end));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.delta_x));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.delta_y));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.delta_xx));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.delta_yy));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.delta_xy));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.err));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.steps));

            Int32 checksum = 0;
            for (int i = 0; i < bytes.Count; i++) {
                checksum += bytes[i];
            }

            //prepend the count byte
            bytes.Insert(0, (byte)bytes.Count);

            //prepend the 10byte startheader
            bytes.InsertRange(0, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF });

            //add checksum
            bytes.AddRange(BitConverter.GetBytes(checksum));
            return bytes.ToArray();
        }

        /*
        void Save() {
            DrawInstruction d = new DrawInstruction();

            //pre-calculate the length of a serialmessage
            byte[] bytes = FormatDrawInstruction(d);
            Console.WriteLine(bytes.Length);

            Int32 version = 2;
            Int64 count = 1020;
            Int64 start = 60;
            byte size = (byte)bytes.Length;
            using (FileStream fileStream = new FileStream("drawings/test" + count.ToString() + ".dri", FileMode.Create)) {
                // Write the data to the file, byte by byte.

                fileStream.Seek(0, SeekOrigin.Begin);
                fileStream.Write(Encoding.ASCII.GetBytes("DRI::DrawInstruction"));
                fileStream.Write(BitConverter.GetBytes(version));
                fileStream.Write(BitConverter.GetBytes(count));
                fileStream.Write(BitConverter.GetBytes(start));
                fileStream.Write(BitConverter.GetBytes(size));

                Random rnd = new Random();
                for (Int64 i = 0; i < count; i++) {
                    d.index = i;
                    d.type = 1;
                    d.dirX = (sbyte)Math.Sign(rnd.Next(-100, 100));
                    d.dirY = (sbyte)Math.Sign(rnd.Next(-100, 100));
                    d.steps = 0;
                    d.startX = -3;
                    d.startY = -30;
                    d.endX = -300;
                    d.endY = -3000;
                    // d.deltaX = rnd.NextInt64(-10000000000, 1000000000000);
                    // d.deltaY = rnd.NextInt64(-10000000000, 1000000000000);
                    // d.deltaXX = rnd.NextInt64(-10000000000, 1000000000000);
                    // d.deltaYY = rnd.NextInt64(-10000000000, 1000000000000);
                    // d.deltaXY = rnd.NextInt64(-10000000000, 1000000000000);
                    // d.error = rnd.NextInt64(-10000000000, 1000000000000);
                    d.deltaX = -3;
                    d.deltaY = -30;
                    d.deltaXX = -300;
                    d.deltaYY = -3000;
                    d.deltaXY = -30000;
                    d.error = -300000;

                    bytes = FormatDrawInstruction(d);
                    fileStream.Seek(size * i + start, SeekOrigin.Begin);
                    fileStream.Write(bytes);
                }
                fileStream.Seek(size * count + start, SeekOrigin.Begin);
                fileStream.WriteByte(0);
            }
        }
*/

        public void addLine(Int64 x_start, Int64 y_start, Int64 x_end, Int64 y_end) {
            //add drawinstruction
            DrawInstruction d = new DrawInstruction();
            d.type = lineType.Straight;
            d.x_start = x_start; d.y_start = y_start; d.x_end = x_end; d.y_end = y_end;
            d.delta_x = Math.Abs(x_end - x_start);
            d.delta_y = -Math.Abs(y_end - y_start);
            d.dir_x = (sbyte)(x_start < x_end ? 1 : -1);
            d.dir_y = (sbyte)(y_start < y_end ? 1 : -1);
            d.err = d.delta_x + d.delta_y;
            d.index = index;
            DrawInstructions.Add(d);
            index++;
        }

        void addBezier(Int64 x0, Int64 y0, Int64 x1, Int64 y1, Int64 x2, Int64 y2) {
            if ((x0 - x1) * (x2 - x1) > 0) {
                SplitBezierHorizontal(x0, y0, x1, y1, x2, y2);
            } else if ((y0 - y1) * (y2 - y1) > 0) {
                SplitBezierVertical(x0, y0, x1, y1, x2, y2);
            } else {
                addBezierSegment(x0, y0, x1, y1, x2, y2);
            }
        }

        void SplitBezierHorizontal(Int64 x0, Int64 y0, Int64 x1, Int64 y1, Int64 x2, Int64 y2) {
            Double t = (x0 - 2 * x1 + x2);
            t = (x0 - x1) / t;
            Int64 ysplit = (Int64)Math.Floor((1 - t) * ((1 - t) * y0 + 2.0 * t * y1) + t * t * y2 + 0.5);
            t = ((x0 * x2 - x1 * x1) * t) / (x0 - x1);
            Int64 xsplit = (Int64)Math.Floor(t + 0.5);
            Int64 ysplitc1 = (Int64)Math.Floor(((y1 - y0) * (t - x0)) / (x1 - x0) + y0 + 0.5);
            Int64 ysplitc2 = (Int64)Math.Floor(((y1 - y2) * (t - x2)) / (x1 - x2) + y2 + 0.5);

            if ((y0 - ysplitc1) * (ysplit - ysplitc1) > 0) {
                SplitBezierVertical(x0, y0, xsplit, ysplitc1, xsplit, ysplit);
            } else {
                addBezierSegment(x0, y0, xsplit, ysplitc1, xsplit, ysplit);
            }

            if ((ysplit - ysplitc2) * (y2 - ysplitc2) > 0) {
                SplitBezierVertical(xsplit, ysplit, xsplit, ysplitc2, x2, y2);
            } else {
                addBezierSegment(xsplit, ysplit, xsplit, ysplitc2, x2, y2);
            }
        }

        void SplitBezierVertical(Int64 x0, Int64 y0, Int64 x1, Int64 y1, Int64 x2, Int64 y2) {
            Double t = y0 - 2 * y1 + y2;
            t = (y0 - y1) / t;
            Int64 xsplit = (Int64)Math.Floor((1 - t) * ((1 - t) * x0 + 2 * t * x1) + t * t * x2 + 0.5);
            t = ((y0 * y2 - y1 * y1) * t) / (y0 - y1);
            Int64 ysplit = (Int64)Math.Floor(t + 0.5);
            Int64 xsplitc1 = (Int64)Math.Floor(((x1 - x0) * (t - y0)) / (y1 - y0) + x0 + 0.5);
            Int64 xsplitc2 = (Int64)Math.Floor(((x1 - x2) * (t - y2)) / (y1 - y2) + x2 + 0.5);

            addBezierSegment(x0, y0, xsplitc1, ysplit, xsplit, ysplit);
            addBezierSegment(xsplit, ysplit, xsplitc2, ysplit, x2, y2);
        }

        void addBezierSegment(Int64 x_start, Int64 y_start, Int64 x_control, Int64 y_control, Int64 x_end, Int64 y_end) {
            //check to make sure there is no gradient change
            if (!((x_start - x_control) * (x_end - x_control) <= 0 && (y_start - y_control) * (y_end - y_control) <= 0)) {
                Console.WriteLine("Curve Changed SIGN!");
                return;
            }
            Int64 dir_x = x_start < x_end ? 1 : -1;
            Int64 dir_y = y_start < y_end ? 1 : -1;

            Int64 temp_y = (x_start - 2 * x_control + x_end);
            Int64 temp_x = (y_start - 2 * y_control + y_end);

            Int64 curve = (temp_y * (y_end - y_start) - temp_x * (x_end - x_start)) * dir_x * dir_y;

            if (curve == 0) {
                Console.WriteLine("Straight Line, no curve");
                //submit 
                addLine(x_start, y_start, x_control, y_control);
                addLine(x_control, y_control, x_end, y_end);
                return;
            }

            Int64 delta_yy = 2 * temp_y * temp_y;
            Int64 delta_xx = 2 * temp_x * temp_x;
            Int64 delta_xy = 2 * temp_x * temp_y * dir_x * dir_y;

            /* error increments for P0*/
            Int64 delta_x = (1 - 2 * Math.Abs(x_start - x_control)) * temp_x * temp_x + Math.Abs(y_start - y_control) * delta_xy - curve * Math.Abs(y_start - y_end);
            Int64 delta_y = (1 - 2 * Math.Abs(y_start - y_control)) * temp_y * temp_y + Math.Abs(x_start - x_control) * delta_xy + curve * Math.Abs(x_start - x_end);

            /* error increments for P2*/
            Int64 delta_x_end = (1 - 2 * Math.Abs(x_end - x_control)) * temp_x * temp_x + Math.Abs(y_end - y_control) * delta_xy + curve * Math.Abs(y_start - y_end);
            Int64 delta_y_end = (1 - 2 * Math.Abs(y_end - y_control)) * temp_y * temp_y + Math.Abs(x_end - x_control) * delta_xy - curve * Math.Abs(x_start - x_end);

            if (curve < 0) {
                /* negated curvature */
                delta_yy = -delta_yy;
                delta_x = -delta_x;
                delta_x_end = -delta_x_end;
                delta_y_end = -delta_y_end;
                delta_xy = -delta_xy;
                delta_xx = -delta_xx;
                delta_y = -delta_y;
            }

            /* algorithm fails for almost straight line, check error values */
            if (delta_x >= -delta_xx || delta_y <= -delta_yy || delta_x_end <= -delta_xx || delta_y_end >= -delta_yy) {
                Console.WriteLine("Almost Straight Line, draw 2 straight lines");
                x_control = (x_start + 4 * x_control + x_end) / 6;
                y_control = (y_start + 4 * y_control + y_end) / 6;
                /* approximation */
                addLine(x_start, y_start, x_control, y_control);
                addLine(x_control, y_control, x_end, y_end);
                return;
            }

            delta_x -= delta_xy;
            Int64 err = delta_x + delta_y; /* error of 1st step */
            delta_y -= delta_xy;

            DrawInstruction d = new DrawInstruction();
            d.type = lineType.QuadraticBezier;
            d.x_start = x_start;
            d.y_start = y_start;
            d.x_end = x_end;
            d.y_end = y_end;
            d.delta_x = delta_x;
            d.delta_y = delta_y;
            d.delta_xx = delta_xx;
            d.delta_yy = delta_yy;
            d.delta_xy = delta_xy;
            d.dir_x = (sbyte)dir_x;
            d.dir_y = (sbyte)dir_y;
            d.err = err;
            d.index = index;
            DrawInstructions.Add(d);
            index++;
        }

        void plotBezier3d(Int64 xStart, Int64 yStart, Int64 zStart, Int64 xControl, Int64 yControl, Int64 zControl, Int64 xEnd, Int64 yEnd, Int64 zEnd) {
            for (var i = 1; i < 4; i++) {                           // split in max 4 segments 
                Double t;

                Double splitX = xStart - 2 * xControl + xEnd;
                if (splitX != 0) splitX = (xStart - xControl) / splitX;

                Double splitY = yStart - 2 * yControl + yEnd;
                if (splitY != 0) splitY = (yStart - yControl) / splitY;

                Double splitZ = zStart - 2 * zControl + zEnd;
                if (splitZ != 0) splitZ = (zStart - zControl) / splitZ;

                t = splitX;                                                        // curve sign change in x axis ?
                if (t <= 0 || (splitY > 0 && splitY < t)) t = splitY;              // curve sign change in y axis ?
                if (t <= 0 || (splitZ > 0 && splitZ < t)) t = splitZ;              // curve sign change in z axis ?

                if (t <= 0 || t >= 1) break;                                       // no more splits

                // Casteljau split at t 
                Int64 x_splitEnd = (Int64)Math.Round((1 - t) * ((1 - t) * xStart + 2 * t * xControl) + t * t * xEnd);
                Int64 y_splitEnd = (Int64)Math.Round((1 - t) * ((1 - t) * yStart + 2 * t * yControl) + t * t * yEnd);
                Int64 z_splitEnd = (Int64)Math.Round((1 - t) * ((1 - t) * zStart + 2 * t * zControl) + t * t * zEnd);
                Int64 x_splitControl = (Int64) Math.Round((1 - t) * xStart + t * xControl);
                Int64 y_splitControl = (Int64) Math.Round((1 - t) * yStart + t * yControl);
                Int64 z_splitControl = (Int64) Math.Round((1 - t) * zStart + t * zControl);

                DrawBezier3DSegment(xStart, yStart, zStart, x_splitControl, y_splitControl, z_splitControl, x_splitEnd, y_splitEnd, z_splitEnd);

                // set up for next loop
                xStart = x_splitEnd;
                yStart = y_splitEnd;
                zStart = z_splitEnd;
                xControl = (Int64)Math.Round((1 - t) * xControl + t * xEnd);
                yControl = (Int64)Math.Round((1 - t) * yControl + t * yEnd);
                zControl = (Int64)Math.Round((1 - t) * zControl + t * zEnd);
            }
            DrawBezier3DSegment(xStart, yStart, zStart, xControl, yControl, zControl, xEnd, yEnd, zEnd);
        }


        void DrawBezier3DSegment(Int64 xStart, Int64 yStart, Int64 zStart, Int64 xControl, Int64 yControl, Int64 zControl, Int64 xEnd, Int64 yEnd, Int64 zEnd) {
            // Console.Write("Drawing Bezier: (");
            // Console.Write(xStart);
            // Console.Write(" ,");
            // Console.Write(yStart);
            // Console.Write(" ,");
            // Console.Write(zStart);
            // Console.Write(" ,");
            // Console.Write(xEnd);
            // Console.Write(" ,");
            // Console.Write(yEnd);
            // Console.Write(" ,");
            // Console.Write(zEnd);
            // Console.WriteLine(")");

            // Int64 sx = xEnd - xControl;
            // Int64 xx = xStart - xControl;
            Int64 dx = Math.Abs(zStart * (xEnd - xControl) + zControl * (xStart - xEnd) - zEnd * (xStart - xControl));

            // Int64 sy = yEnd - yControl;
            // Int64 yy = yStart - yControl;
            Int64 dy = Math.Abs(zStart * (yEnd - yControl) + zControl * (yStart - yEnd) - zEnd * (yStart - yControl));

            Int64 xy = Math.Abs(xStart * (yEnd - yControl) + xControl * (yStart - yEnd) - xEnd * (yStart - yControl));

            uint projection = 0;                    // 3d plane orientation (xy)

            if (dx > xy && dx > dy) {
                projection=1;                       // 3d plane orientation (xz)
            } else if (dy > xy) {
                projection=2;                       // 3d plane orientation (zy)
            }

            if (projection!=0) {
                Int64 tzStart     = zStart;
                Int64 tzControl   = zControl;
                Int64 tzEnd       = zEnd;
                if (projection==1) {                // swap y <-> z axis
                    zStart   = yStart;
                    zControl = yControl;
                    zEnd     = yEnd;
                    yStart   = tzStart;
                    yControl = tzControl;
                    yEnd     = tzEnd;
                }
                if (projection==2) {                // swap y <-> z axis
                    zStart   = xStart;
                    zControl = xControl;
                    zEnd     = xEnd;
                    xStart   = tzStart;
                    xControl = tzControl;
                    xEnd     = tzEnd;
                }
            }
            
            Int64 sx = xEnd - xControl;
            Int64 xx = xStart - xControl;
            Int64 sy = yEnd - yControl;
            Int64 yy = yStart - yControl;

            //assert(xx*sx <= 0 && yy*sy <= 0 && (z0-z1)*(z2-z1) <= 0); // no sign change */

            // if (dx > xy && dx > dy) {               // set curve plane to x-y
            //     yStart = zStart;                    // swap y <-> z axis
            //     zStart = yy + yControl;
            //     yControl = zControl;
            //     zControl = zStart - yy;
            //     yEnd = zEnd;
            //     zEnd = sy + zControl;
            //     sy = yEnd - yControl;
            //     yy = yStart - yControl;
            //     projection = 1;                       
            // } else if (dy > xy) {
            //     xStart = zStart;
            //     zStart = xx + xControl;
            //     xControl = zControl;
            //     zControl = zStart - xx;
            //     xEnd = zEnd;
            //     zEnd = sx + zControl;
            //     sx = xEnd - xControl;
            //     xx = xStart - xControl;
            //     projection = 2;                       // swap x <-> z axis
            // }

            Int64 err;
            Int64 cur = xx * sy - yy * sx;

            if (cur == 0) { // no curve straight line
                DrawLine3D(xStart, yStart, zStart, xEnd, yEnd, zEnd);
            } else {
                //begin with shorter part
                // if (sx * sx + sy * sy > xx * xx + yy * yy) {
                //     // swap start - end       
                //     xEnd = zStart;
                //     zStart = zEnd;
                //     zEnd = xEnd;
                //     xEnd = xStart;
                //     xStart = sx + xControl;
                //     yEnd = yStart;
                //     yStart = sy + yControl;
                //     cur = -cur;
                // }


                int dirX = xStart < xEnd ? 1 : -1;                // x step direction 
                int dirY = yStart < yEnd ? 1 : -1;                // y step direction 

                xx += sx;
                xx *= dirX;
                yy += sy;
                yy *= dirY;
                xy = 2 * xx * yy;
                xx *= xx;
                yy *= yy;                                       // differences 2nd degree 
                if (cur * dirX * dirY < 0) {                        // negated curvature? 
                    xx = -xx;
                    yy = -yy;
                    xy = -xy;
                    cur = -cur;
                }

                dx = 4 * dirY * cur * (xControl - xStart) + xx - xy;        // differences 1st degree
                dy = 4 * dirX * cur * (yStart - yControl) + yy - xy;
                xx += xx;
                yy += yy;
                err = dx + dy + xy;                                         // error 1st step */

                Int64 x = xStart;
                Int64 y = yStart;
                Int64 z = zStart;

                Int64 ex,ey,ez,dz;
                if (zEnd != zStart) {
                    ex = Math.Abs((yStart - yControl) * zEnd + (yEnd - yStart) * zControl - (yEnd - yControl) * zStart);    // x part of surface normal 
                    ey = Math.Abs((xStart - xControl) * zEnd + (xEnd - xStart) * zControl - (xEnd - xControl) * zStart);    // y part of surface normal 
                    dz = (ex * Math.Abs(xEnd - xStart) + ey * Math.Abs(yEnd - yStart)) / Math.Abs(zEnd - zStart);
                    ez = dz / 2;
                } else {
                    ex = 0;
                    ey = 0;
                    ez = 0;
                    dz = 0;                    
                }
                int dirZ = zStart < zEnd ? 1 : -1;                // z step direction

                do {
                    //plot curve
                    if (projection == 0) SetPixel3D(x, y, z);
                    if (projection == 1) SetPixel3D(x, z, y);
                    if (projection == 2) SetPixel3D(z, y, x);

                    if (x == xEnd && y == yEnd) return;                     // last pixel -> curve finished 

                    bool yStep = 2 * err < dx;                              // test for y step 
                    bool xStep = 2 * err > dy;                              // test for x step 

                    if (xStep) {
                        x += dirX;                                          // x step 
                        dx -= xy;
                        dy += yy;
                        err += dy;
                        ez -= ex;
                    }

                    if (yStep) {
                        y += dirY;                                          // y step 
                        dy -= xy;
                        dx += xx;
                        err += dx;
                        ez -= ey;
                    }

                    if (ez < 0) {
                        ez += dz;
                        z += dirZ;                                          // z step 
                    }
                } while (dy < dx);                                          // gradient negates -> algorithm fails

                /* plot remaining part as straight line to end */
                if (projection == 0) DrawLine3D(x, y, z, xEnd, yEnd, zEnd);
                if (projection == 1) DrawLine3D(x, z, y, xEnd, zEnd, yEnd);
                if (projection == 2) DrawLine3D(z, y, x, zEnd, yEnd, xEnd);
            }
        }

        void draw(DrawInstruction d) {
            Int64 x = d.x_start;
            Int64 y = d.y_start;
            Int64 err = d.err;
            d.steps = 0;

            if (d.type == lineType.Straight) {
                SetPixel(x, y);
                while (x != d.x_end && y != d.y_end) {
                    if (2 * err <= d.delta_x) {
                        err += d.delta_x;
                        y += d.dir_y;
                    }
                    if (2 * err >= d.delta_y) {
                        err += d.delta_y;
                        x += d.dir_x;
                    }
                    SetPixel(x, y);
                }

                while (x != d.x_end) {
                    x += d.dir_x;
                    SetPixel(x, y);
                }
                while (y != d.y_end) {
                    y += d.dir_y;
                    SetPixel(x, y);
                }
            }
            if (d.type == lineType.Quadratic3DBezier) {
            }

            if (d.type == lineType.QuadraticBezier) {
                Int64 delta_x = d.delta_x;
                Int64 delta_y = d.delta_y;
                while (x != d.x_end && y != d.y_end) {
                    bool step_x = 2 * err - delta_x >= 0;
                    bool step_y = 2 * err - delta_y <= 0;

                    // bool step_x = 2 * err - d.delta_x >= 0;
                    // bool step_y = 2 * err - d.delta_y <= 0;
                    SetPixel(x, y);

                    if (step_x) {
                        x += d.dir_x;
                        delta_y -= d.delta_xy;
                        delta_x += d.delta_xx;
                        err += delta_x;
                    }
                    if (step_y) {
                        y += d.dir_y;
                        delta_x -= d.delta_xy;
                        delta_y += d.delta_yy;
                        err += delta_y;
                    }
                }
                SetPixel(x, y);

                // //at least x or y has reached its final position, it anything remains, it must be a straight line
                while (x != d.x_end) {
                    x += d.dir_x;
                    SetPixel(x, y);
                }
                while (y != d.y_end) {
                    y += d.dir_y;
                    SetPixel(x, y);
                }
            }

        }

        void DrawLine3D(Int64 xStart, Int64 yStart, Int64 zStart, Int64 xEnd, Int64 yEnd, Int64 zEnd) {
            // Console.Write("Drawing StraightLine: (");
            // Console.Write(xStart);
            // Console.Write(" ,");
            // Console.Write(yStart);
            // Console.Write(" ,");
            // Console.Write(zStart);
            // Console.Write(" ,");
            // Console.Write(xEnd);
            // Console.Write(" ,");
            // Console.Write(yEnd);
            // Console.Write(" ,");
            // Console.Write(zEnd);
            // Console.WriteLine(")");

            Int64 deltaX = Math.Abs(xEnd - xStart);
            int dirX = xStart < xEnd ? 1 : -1;

            Int64 deltaY = Math.Abs(yEnd - yStart);
            int dirY = yStart < yEnd ? 1 : -1;

            Int64 deltaZ = Math.Abs(zEnd - zStart);
            int dirZ = zStart < zEnd ? 1 : -1;

            Int64 deltaMax = Math.Max(deltaZ, Math.Max(deltaX, deltaY));
            
            xEnd = yEnd = zEnd = deltaMax / 2;

            Int64 x = xStart;
            Int64 y = yStart;
            Int64 z = zStart;

            Int64 xErr = xEnd;
            Int64 yErr = yEnd;
            Int64 zErr = zEnd;

            for (Int64 i = deltaMax; i >= 0; i--) {
                SetPixel3D(x, y, z);
                xErr -= deltaX; 
                if (xErr < 0) { 
                    xErr += deltaMax; 
                    x += dirX; 
                }
                
                yErr -= deltaY; 
                if (yErr < 0) { 
                    yErr += deltaMax; 
                    y += dirY; 
                    }

                zErr -= deltaZ; 
                if (zErr < 0) { 
                    zErr += deltaMax; 
                    z += dirZ; 
                }
            }
        }


        void SetPixel3D(Int64 x, Int64 y, Int64 z) {
            Console.WriteLine(String.Format("({0}, {1}, {2})", x, y, z));
            // if (Math.Abs(x - lastX) > 1 || Math.Abs(y - lastY) > 1) {
            //     Console.WriteLine(String.Format("({0}, {1})", lastX, lastY));
            //     Console.WriteLine(String.Format("({0}, {1})", x, y));
            //     Console.WriteLine("Jump Occured!");
            // }
            lastX = x;
            lastY = y;

            Dot dxy = new Dot();
            dxy.layer = 0;
            dxy.size = 0.025f * z;
            dxy.color = Veldrid.RgbaFloat.Black;
            dxy.position = new Vector2(x, y);
            Data.dots.Add(dxy);

            Dot dxz = new Dot();
            dxz.layer = 0;
            dxz.size = 0.5f;
            dxz.color = Veldrid.RgbaFloat.Black;
            dxz.position = new Vector2(x, z + 100);
            Data.dots.Add(dxz);

            Dot dzy = new Dot();
            dzy.layer = 0;
            dzy.size = 0.5f;
            dzy.color = Veldrid.RgbaFloat.Black;
            dzy.position = new Vector2(z + 100, y);
            Data.dots.Add(dzy);

            Dot dyz = new Dot();
            dyz.layer = 0;
            dyz.size = 0.5f;
            dyz.color = Veldrid.RgbaFloat.Black;
            dyz.position = new Vector2(y + 100, z + 100);
            Data.dots.Add(dyz);

            dotIndex++;
        }

        void SetPixel(Int64 x, Int64 y) {
            if (Math.Abs(x - lastX) > 1 || Math.Abs(y - lastY) > 1) {
                Console.WriteLine(String.Format("({0}, {1})", lastX, lastY));
                Console.WriteLine(String.Format("({0}, {1})", x, y));
                Console.WriteLine("Jump Occured!");
            }

            lastX = x;
            lastY = y;

            Dot d = new Dot();
            d.layer = 0;
            d.size = 10000.0f;
            d.color = Veldrid.RgbaFloat.Black;
            d.position = new Vector2(x, y);

            Data.dots.Add(d);
            dotIndex++;
        }

    }

}