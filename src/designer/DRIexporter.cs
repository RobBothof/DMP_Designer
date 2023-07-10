using System.Numerics;
using System.Text;

namespace Designer
{

    public class DriExporter
    {
        public List<DrawInstruction> DrawInstructions;
        private UInt64 index = 0;
        private UInt64 dotIndex = 0;
        private Int64 lastX = 0;
        private Int64 lastY = 0;

        public DriExporter()
        {
            DrawInstructions = new List<DrawInstruction>();
        }

        public void Export(String path)
        {
            // start new file
            // string path = "drawings/" + exportfilename + ".dri";
            // string path = exportfilename + ".dri";
            DrawInstruction dtemp = new DrawInstruction();
            byte[] bytes = FormatDrawInstruction(dtemp);
            Int32 version = 2;
            Int64 start = 60;
            byte size = (byte)bytes.Length;

            using (FileStream fileStream = new FileStream(path, FileMode.Create))
            {
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
            foreach (Line l in Data.lines)
            {
                DrawInstructions.Clear();

                if (l.type == lineType.QuadraticBezier3D)
                {
                    AddBezier3D(
                        (Int64)l.points[0].X, (Int64)l.points[0].Y, (Int64)l.points[0].Z,
                        (Int64)l.points[1].X, (Int64)l.points[1].Y, (Int64)l.points[1].Z,
                        (Int64)l.points[2].X, (Int64)l.points[2].Y, (Int64)l.points[2].Z
                    );
                }

                if (l.type == lineType.Straight3D)
                {
                    AddLine3D(
                        (Int64)l.points[0].X, (Int64)l.points[0].Y, (Int64)l.points[0].Z,
                        (Int64)l.points[1].X, (Int64)l.points[1].Y, (Int64)l.points[1].Z
                    );
                }

                //save all instructions in the list

                foreach (DrawInstruction d in DrawInstructions)
                {
                    using (FileStream fileStream = new FileStream(path, FileMode.Open))
                    {
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

            using (FileStream fileStream = new FileStream(path, FileMode.Open))
            {
                fileStream.Seek(24, SeekOrigin.Begin);
                fileStream.Write(BitConverter.GetBytes(instructioncount));
                fileStream.Seek(start + instructioncount * size, SeekOrigin.Begin);
                fileStream.WriteByte(0);
            }
        }


        byte[] FormatDrawInstruction(DrawInstruction d)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(d.index));
            bytes.Add(0);
            bytes.Add((byte)d.type);
            bytes.Add(0);
            bytes.Add((byte)d.accelType);
            bytes.Add(0);
            bytes.Add((byte)d.dirX);
            bytes.Add(0);
            bytes.Add((byte)d.dirY);
            bytes.Add(0);
            bytes.Add((byte)d.dirZ);
            bytes.Add(0);
            bytes.Add((byte)d.projection);
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.startX));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.startY));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.startZ));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.endX));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.endY));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.endZ));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.deltaX));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.deltaY));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.deltaZ));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.deltaXX));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.deltaYY));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.deltaXY));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.deltaXZ));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.deltaYZ));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.err));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.errZ));
            bytes.Add(0);
            bytes.AddRange(BitConverter.GetBytes(d.steps));

            Int32 checksum = 0;
            for (int i = 0; i < bytes.Count; i++)
            {
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

        void AddBezier3D(Int64 startX, Int64 startY, Int64 startZ, Int64 controlX, Int64 controlY, Int64 controlZ, Int64 endX, Int64 endY, Int64 endZ)
        {
            for (var i = 1; i < 4; i++)
            {                                           // split in max 4 segments 
                Double t;

                Double splitX = startX - 2 * controlX + endX;
                if (splitX != 0) splitX = (startX - controlX) / splitX;

                Double splitY = startY - 2 * controlY + endY;
                if (splitY != 0) splitY = (startY - controlY) / splitY;

                Double splitZ = startZ - 2 * controlZ + endZ;
                if (splitZ != 0) splitZ = (startZ - controlZ) / splitZ;

                t = splitX;                                                         // curve sign change in x axis ?
                if (t <= 0 || (splitY > 0 && splitY < t)) t = splitY;               // curve sign change in y axis ?
                if (t <= 0 || (splitZ > 0 && splitZ < t)) t = splitZ;               // curve sign change in z axis ?

                if (t <= 0 || t >= 1) break;                                        // no more splits

                // Casteljau split at t 
                Int64 endSplitX = (Int64)Math.Round((1 - t) * ((1 - t) * startX + 2 * t * controlX) + t * t * endX);
                Int64 endSplitY = (Int64)Math.Round((1 - t) * ((1 - t) * startY + 2 * t * controlY) + t * t * endY);
                Int64 endSplitZ = (Int64)Math.Round((1 - t) * ((1 - t) * startZ + 2 * t * controlZ) + t * t * endZ);
                Int64 controlSplitX = (Int64)Math.Round((1 - t) * startX + t * controlX);
                Int64 controlSplitY = (Int64)Math.Round((1 - t) * startY + t * controlY);
                Int64 controlSplitZ = (Int64)Math.Round((1 - t) * startZ + t * controlZ);

                AddBezier3DSegment(startX, startY, startZ, controlSplitX, controlSplitY, controlSplitZ, endSplitX, endSplitY, endSplitZ);

                // set up for next loop
                startX = endSplitX;
                startY = endSplitY;
                startZ = endSplitZ;
                controlX = (Int64)Math.Round((1 - t) * controlX + t * endX);
                controlY = (Int64)Math.Round((1 - t) * controlY + t * endY);
                controlZ = (Int64)Math.Round((1 - t) * controlZ + t * endZ);
            }
            AddBezier3DSegment(startX, startY, startZ, controlX, controlY, controlZ, endX, endY, endZ);
        }

        void AddBezier3DSegment(Int64 startX, Int64 startY, Int64 startZ, Int64 controlX, Int64 controlY, Int64 controlZ, Int64 endX, Int64 endY, Int64 endZ)
        {
            Console.Write("Drawing 3D Curve: (");
            Console.Write(startX);
            Console.Write(" ,");
            Console.Write(startY);
            Console.Write(" ,");
            Console.Write(startZ);
            Console.Write(" ,");
            Console.Write(endX);
            Console.Write(" ,");
            Console.Write(endY);
            Console.Write(" ,");
            Console.Write(endZ);
            Console.WriteLine(")");

            //// We look at the 2D projections , determine which is the longest curve
            Int64 normalXZ = Math.Abs(startZ * (endX - controlX) + controlZ * (startX - endX) - endZ * (startX - controlX));
            Int64 normalYZ = Math.Abs(startZ * (endY - controlY) + controlZ * (startY - endY) - endZ * (startY - controlY));
            Int64 normalXY = Math.Abs(startX * (endY - controlY) + controlX * (startY - endY) - endX * (startY - controlY));

            uint projection = 1;                                            // 3d plane orientation (xy)
            if (normalXZ > normalXY && normalXZ > normalYZ) projection = 2; // 3d plane orientation (xz)
            if (normalYZ > normalXY && normalYZ > normalXZ) projection = 3; // 3d plane orientation (zy)

            if (projection == 2)
            {                // swap y <-> z axis
                Int64 tstartZ = startZ;
                Int64 tcontrolZ = controlZ;
                Int64 tendZ = endZ;
                startZ = startY;
                controlZ = controlY;
                endZ = endY;
                startY = tstartZ;
                controlY = tcontrolZ;
                endY = tendZ;
            }

            if (projection == 3) // swap y <-> z axis
            {
                Int64 tstartZ = startZ;
                Int64 tcontrolZ = controlZ;
                Int64 tendZ = endZ;

                startZ = startX;
                controlZ = controlX;
                endZ = endX;
                startX = tstartZ;
                controlX = tcontrolZ;
                endX = tendZ;
            }


            Int64 err;
            Int64 cur = (startX - controlX) * (endY - controlY) - (startY - controlY) * (endX - controlX);

            if (cur == 0)
            { // no curve || straight line
                if (projection == 1) AddLine3D(startX, startY, startZ, endX, endY, endZ);
                if (projection == 2) AddLine3D(startX, startZ, startY, endX, endZ, endY);
                if (projection == 3) AddLine3D(startZ, startY, startX, endZ, endY, endX);
            }
            else
            {
                //skip :: begin with shorter part

                int dirX = startX < endX ? 1 : -1;                              // x step direction 
                int dirY = startY < endY ? 1 : -1;                              // y step direction 

                Int64 deltaXX = (startX - controlX + endX - controlX) * dirX;
                Int64 deltaYY = (startY - controlY + endY - controlY) * dirY;
                Int64 deltaXY = 2 * deltaXX * deltaYY;

                deltaXX *= deltaXX;
                deltaYY *= deltaYY;                                                           // differences 2nd degree 

                if (cur * dirX * dirY < 0)
                {                                                    // negated curvature? 
                    deltaXX = -deltaXX;
                    deltaYY = -deltaYY;
                    deltaXY = -deltaXY;
                    cur = -cur;
                }

                Int64 deltaX = 4 * dirY * cur * (controlX - startX) + deltaXX - deltaXY;     // differences 1st degree
                Int64 deltaY = 4 * dirX * cur * (startY - controlY) + deltaYY - deltaXY;
                deltaXX += deltaXX;
                deltaYY += deltaYY;
                err = deltaX + deltaY + deltaXY;                                             // error 1st step */

                Int64 deltaXZ, deltaYZ, errZ, deltaZ;
                if (endZ != startZ)
                {
                    deltaXZ = Math.Abs((startY - controlY) * endZ + (endY - startY) * controlZ - (endY - controlY) * startZ);    // x part of surface normal 
                    deltaYZ = Math.Abs((startX - controlX) * endZ + (endX - startX) * controlZ - (endX - controlX) * startZ);    // y part of surface normal 
                    deltaZ = (deltaXZ * Math.Abs(endX - startX) + deltaYZ * Math.Abs(endY - startY)) / Math.Abs(endZ - startZ);
                    errZ = deltaZ / 2;
                }
                else
                {
                    deltaXZ = 0;
                    deltaYZ = 0;
                    errZ = 0;
                    deltaZ = 0;
                }
                int dirZ = startZ < endZ ? 1 : -1;                          // z step direction

                DrawInstruction d = new DrawInstruction();
                d.type = lineType.QuadraticBezier3D;
                d.accelType = 0;
                d.dirX = (sbyte)dirX;
                d.dirY = (sbyte)dirY;
                d.dirZ = (sbyte)dirZ;
                d.projection = (byte)projection;
                d.startX = startX;
                d.startY = startY;
                d.startZ = startZ;
                d.endX = endX;
                d.endY = endY;
                d.endZ = endZ;
                d.deltaX = deltaX;
                d.deltaY = deltaY;
                d.deltaZ = deltaZ;
                d.deltaXX = deltaXX;
                d.deltaYY = deltaYY;
                d.deltaXY = deltaXY;
                d.deltaXZ = deltaXZ;
                d.deltaYZ = deltaYZ;
                d.err = err;
                d.errZ = errZ;

                /// Simulate Draw, to calculate number of steps needed for this instruction

                Int64 x = startX;
                Int64 y = startY;
                Int64 z = startZ;

                //we can skip this when actually plotting, as we are already at Start Position.
                if (projection == 1) SetPixel3D(x, y, z);
                if (projection == 2) SetPixel3D(x, z, y);
                if (projection == 3) SetPixel3D(z, y, x);

                UInt64 steps = 0;

                while (x != endX && y != endY)
                {
                    bool stepX = 2 * err > deltaY;                              // test for x step 
                    bool stepY = 2 * err < deltaX;                              // test for y step 

                    if (stepX)
                    {
                        x += dirX;                                          // x step 
                        deltaX -= deltaXY;
                        deltaY += deltaYY;
                        err += deltaY;
                        errZ -= deltaXZ;
                    }

                    if (stepY)
                    {
                        y += dirY;                                          // y step 
                        deltaY -= deltaXY;
                        deltaX += deltaXX;
                        err += deltaX;
                        errZ -= deltaYZ;
                    }

                    if (errZ < 0)
                    {
                        errZ += deltaZ;
                        z += dirZ;                                          // z step 
                    }

                    if (projection == 1) SetPixel3D(x, y, z);
                    if (projection == 2) SetPixel3D(x, z, y);
                    if (projection == 3) SetPixel3D(z, y, x);

                    steps++;
                }

                if (x != endX || y != endY || z != endZ)
                {
                    Console.WriteLine("finishing curve");

                    deltaX = Math.Abs(endX - x);
                    deltaY = Math.Abs(endY - y);
                    deltaZ = Math.Abs(endZ - z);
                    Int64 deltaMax = Math.Max(deltaZ, Math.Max(deltaX, deltaY));

                    Int64 errX, errY;
                    errX = errY = errZ = deltaMax / 2; ;

                    for (Int64 i = deltaMax; i > 0; i--)
                    {
                        errX -= deltaX;
                        if (errX < 0)
                        {
                            errX += deltaMax;
                            x += dirX;                                      // x step
                        }

                        errY -= deltaY;
                        if (errY < 0)
                        {
                            errY += deltaMax;
                            y += dirY;                                      // y step
                        }

                        errZ -= deltaZ;
                        if (errZ < 0)
                        {
                            errZ += deltaMax;
                            z += dirZ;                                      // z step
                        }

                        steps++;

                        if (projection == 1) SetPixel3D(x, y, z);
                        if (projection == 2) SetPixel3D(x, z, y);
                        if (projection == 3) SetPixel3D(z, y, x);
                    }
                }

                Console.Write("finished curve in ");
                Console.Write(steps);
                Console.WriteLine(" steps.");

                d.index = index;
                d.steps = steps;
                DrawInstructions.Add(d);

                index++;
            }
        }


        void AddLine3D(Int64 startX, Int64 startY, Int64 startZ, Int64 endX, Int64 endY, Int64 endZ)
        {
            Console.Write("Drawing 3D Line: (");
            Console.Write(startX);
            Console.Write(" ,");
            Console.Write(startY);
            Console.Write(" ,");
            Console.Write(startZ);
            Console.Write(" ,");
            Console.Write(endX);
            Console.Write(" ,");
            Console.Write(endY);
            Console.Write(" ,");
            Console.Write(endZ);
            Console.WriteLine(")");

            Int64 deltaX = Math.Abs(endX - startX);
            int dirX = startX < endX ? 1 : -1;

            Int64 deltaY = Math.Abs(endY - startY);
            int dirY = startY < endY ? 1 : -1;

            Int64 deltaZ = Math.Abs(endZ - startZ);
            int dirZ = startZ < endZ ? 1 : -1;

            DrawInstruction d = new DrawInstruction();
            d.type = lineType.Straight3D;
            d.accelType = 0;
            d.dirX = (sbyte)dirX;
            d.dirY = (sbyte)dirY;
            d.dirZ = (sbyte)dirZ;
            d.projection = 0;
            d.startX = startX;
            d.startY = startY;
            d.startZ = startZ;
            d.endX = endX;
            d.endY = endY;
            d.endZ = endZ;
            d.deltaX = deltaX;
            d.deltaY = deltaY;
            d.deltaZ = deltaZ;
            d.deltaXX = 0;
            d.deltaYY = 0;
            d.deltaXY = 0;
            d.deltaXZ = 0;
            d.deltaYZ = 0;
            d.err = 0;
            d.errZ = 0;

            /// Simulate Draw, to calculate number of steps needed for this instruction

            Int64 deltaMax = Math.Max(deltaZ, Math.Max(deltaX, deltaY));

            Int64 x = startX;
            Int64 y = startY;
            Int64 z = startZ;

            Int64 errX = deltaMax / 2;
            Int64 errY = deltaMax / 2;
            Int64 errZ = deltaMax / 2;

            UInt64 steps = 0;

            for (Int64 i = deltaMax; i > 0; i--)
            {
                errX -= deltaX;
                if (errX < 0)
                {
                    errX += deltaMax;
                    x += dirX;
                }

                errY -= deltaY;
                if (errY < 0)
                {
                    errY += deltaMax;
                    y += dirY;
                }

                errZ -= deltaZ;
                if (errZ < 0)
                {
                    errZ += deltaMax;
                    z += dirZ;
                }
                SetPixel3D(x, y, z);
                steps++;
            }

            Console.Write("finished straight line in ");
            Console.Write(steps);
            Console.WriteLine(" steps.");

            d.index = index;
            d.steps = steps;
            DrawInstructions.Add(d);

            index++;

        }

        void SetPixel3D(Int64 x, Int64 y, Int64 z)
        {
            /*
            Console.WriteLine(String.Format("({0}, {1}, {2})", x, y, z));
            if (Math.Abs(x - lastX) > 1 || Math.Abs(y - lastY) > 1) {
                Console.WriteLine(String.Format("({0}, {1})", lastX, lastY));
                Console.WriteLine(String.Format("({0}, {1})", x, y));
                Console.WriteLine("Jump Occured!");
            }
            lastX = x;
            lastY = y;

            Dot dxy = new Dot();
            dxy.layer = 0;
            dxy.size = 0.025f * z;
            dxy.color = Veldrid.RgbaFloat.Black;
            dxy.position = new Vector2(x, y);
            Data.dots.Add(dxy);
            dotIndex++;
            */
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

        /*
                public void addLine(Int64 startX, Int64 startY, Int64 endX, Int64 endY) {
                    //add drawinstruction
                    DrawInstruction d = new DrawInstruction();
                    d.type = lineType.Straight;
                    d.startX = startX; d.startY = startY; d.endX = endX; d.endY = endY;
                    d.deltaX = Math.Abs(endX - startX);
                    d.deltaY = -Math.Abs(endY - startY);
                    d.dirX = (sbyte)(startX < endX ? 1 : -1);
                    d.dirY = (sbyte)(startY < endY ? 1 : -1);
                    d.err = d.deltaX + d.deltaY;
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

                void addBezierSegment(Int64 startX, Int64 startY, Int64 x_control, Int64 y_control, Int64 endX, Int64 endY) {
                    //check to make sure there is no gradient change
                    if (!((startX - x_control) * (endX - x_control) <= 0 && (startY - y_control) * (endY - y_control) <= 0)) {
                        Console.WriteLine("Curve Changed SIGN!");
                        return;
                    }
                    Int64 dirX = startX < endX ? 1 : -1;
                    Int64 dirY = startY < endY ? 1 : -1;

                    Int64 temp_y = (startX - 2 * x_control + endX);
                    Int64 temp_x = (startY - 2 * y_control + endY);

                    Int64 curve = (temp_y * (endY - startY) - temp_x * (endX - startX)) * dirX * dirY;

                    if (curve == 0) {
                        Console.WriteLine("Straight Line, no curve");
                        //submit 
                        addLine(startX, startY, x_control, y_control);
                        addLine(x_control, y_control, endX, endY);
                        return;
                    }

                    Int64 deltaYY = 2 * temp_y * temp_y;
                    Int64 deltaXX = 2 * temp_x * temp_x;
                    Int64 deltaXY = 2 * temp_x * temp_y * dirX * dirY;

                    // error increments for P0
                    Int64 deltaX = (1 - 2 * Math.Abs(startX - x_control)) * temp_x * temp_x + Math.Abs(startY - y_control) * deltaXY - curve * Math.Abs(startY - endY);
                    Int64 deltaY = (1 - 2 * Math.Abs(startY - y_control)) * temp_y * temp_y + Math.Abs(startX - x_control) * deltaXY + curve * Math.Abs(startX - endX);

                    // error increments for P2
                    Int64 deltaX_end = (1 - 2 * Math.Abs(endX - x_control)) * temp_x * temp_x + Math.Abs(endY - y_control) * deltaXY + curve * Math.Abs(startY - endY);
                    Int64 deltaY_end = (1 - 2 * Math.Abs(endY - y_control)) * temp_y * temp_y + Math.Abs(endX - x_control) * deltaXY - curve * Math.Abs(startX - endX);

                    if (curve < 0) {
                        // negated curvature
                        deltaYY = -deltaYY;
                        deltaX = -deltaX;
                        deltaX_end = -deltaX_end;
                        deltaY_end = -deltaY_end;
                        deltaXY = -deltaXY;
                        deltaXX = -deltaXX;
                        deltaY = -deltaY;
                    }

                    // algorithm fails for almost straight line, check error values 
                    if (deltaX >= -deltaXX || deltaY <= -deltaYY || deltaX_end <= -deltaXX || deltaY_end >= -deltaYY) {
                        Console.WriteLine("Almost Straight Line, draw 2 straight lines");
                        x_control = (startX + 4 * x_control + endX) / 6;
                        y_control = (startY + 4 * y_control + endY) / 6;
                        // approximation 
                        addLine(startX, startY, x_control, y_control);
                        addLine(x_control, y_control, endX, endY);
                        return;
                    }

                    deltaX -= deltaXY;
                    Int64 err = deltaX + deltaY; // error of 1st step 
                    deltaY -= deltaXY;

                    DrawInstruction d = new DrawInstruction();
                    d.type = lineType.QuadraticBezier;
                    d.startX = startX;
                    d.startY = startY;
                    d.endX = endX;
                    d.endY = endY;
                    d.deltaX = deltaX;
                    d.deltaY = deltaY;
                    d.deltaXX = deltaXX;
                    d.deltaYY = deltaYY;
                    d.deltaXY = deltaXY;
                    d.dirX = (sbyte)dirX;
                    d.dirY = (sbyte)dirY;
                    d.err = err;
                    d.index = index;
                    DrawInstructions.Add(d);
                    index++;
                }
        */

        /*
        void draw(DrawInstruction d) {
            Int64 x = d.startX;
            Int64 y = d.startY;
            Int64 err = d.err;
            d.steps = 0;

            if (d.type == lineType.Straight) {
                SetPixel(x, y);
                while (x != d.endX && y != d.endY) {
                    if (2 * err <= d.deltaX) {
                        err += d.deltaX;
                        y += d.dirY;
                    }
                    if (2 * err >= d.deltaY) {
                        err += d.deltaY;
                        x += d.dirX;
                    }
                    SetPixel(x, y);
                }

                while (x != d.endX) {
                    x += d.dirX;
                    SetPixel(x, y);
                }
                while (y != d.endY) {
                    y += d.dirY;
                    SetPixel(x, y);
                }
            }
            if (d.type == lineType.Quadratic3DBezier) {
            }

            if (d.type == lineType.QuadraticBezier) {
                Int64 deltaX = d.deltaX;
                Int64 deltaY = d.deltaY;
                while (x != d.endX && y != d.endY) {
                    bool step_x = 2 * err - deltaX >= 0;
                    bool step_y = 2 * err - deltaY <= 0;

                    // bool step_x = 2 * err - d.deltaX >= 0;
                    // bool step_y = 2 * err - d.deltaY <= 0;
                    SetPixel(x, y);

                    if (step_x) {
                        x += d.dirX;
                        deltaY -= d.deltaXY;
                        deltaX += d.deltaXX;
                        err += deltaX;
                    }
                    if (step_y) {
                        y += d.dirY;
                        deltaX -= d.deltaXY;
                        deltaY += d.deltaYY;
                        err += deltaY;
                    }
                }
                SetPixel(x, y);

                // //at least x or y has reached its final position, it anything remains, it must be a straight line
                while (x != d.endX) {
                    x += d.dirX;
                    SetPixel(x, y);
                }
                while (y != d.endY) {
                    y += d.dirY;
                    SetPixel(x, y);
                }
            }

        }
        */




        /*
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
        */

    }

}