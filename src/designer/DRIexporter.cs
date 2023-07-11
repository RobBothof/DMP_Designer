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
        private Int64 lastZ = 0;

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

                if (l.type == lineType.QuadraticBezier)
                {
                    AddBezier(
                        (Int64)l.points[0].X, (Int64)l.points[0].Y, (Int64)l.points[0].Z,
                        (Int64)l.points[1].X, (Int64)l.points[1].Y, (Int64)l.points[1].Z,
                        (Int64)l.points[2].X, (Int64)l.points[2].Y, (Int64)l.points[2].Z
                    );
                }

                if (l.type == lineType.Straight)
                {
                    AddLine(
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
            Console.Write("\nWrote:");
            Console.Write(instructioncount);
            Console.WriteLine(" drawinstructions");

            Data.DebugConsole.Add("Wrote: " + instructioncount.ToString() + " drawinstructions");

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

        void AddBezier(Int64 startX, Int64 startY, Int64 startZ, Int64 controlX, Int64 controlY, Int64 controlZ, Int64 endX, Int64 endY, Int64 endZ)
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

                Console.WriteLine("\nSplitting Curve.");
                AddBezierSegment(startX, startY, startZ, controlSplitX, controlSplitY, controlSplitZ, endSplitX, endSplitY, endSplitZ);

                // set up for next loop
                startX = endSplitX;
                startY = endSplitY;
                startZ = endSplitZ;
                controlX = (Int64)Math.Round((1 - t) * controlX + t * endX);
                controlY = (Int64)Math.Round((1 - t) * controlY + t * endY);
                controlZ = (Int64)Math.Round((1 - t) * controlZ + t * endZ);
            }
            AddBezierSegment(startX, startY, startZ, controlX, controlY, controlZ, endX, endY, endZ);
        }

        void AddBezierSegment(Int64 startX, Int64 startY, Int64 startZ, Int64 controlX, Int64 controlY, Int64 controlZ, Int64 endX, Int64 endY, Int64 endZ)
        {
            Console.Write("\nCalculating Curve: (");
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

            if (projection == 2) // swap y <-> z axis
            {                
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

            if (projection == 3) // swap x <-> z axis
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
                if (projection == 1) AddLine(startX, startY, startZ, endX, endY, endZ);
                if (projection == 2) AddLine(startX, startZ, startY, endX, endZ, endY);
                if (projection == 3) AddLine(startZ, startY, startX, endZ, endY, endX);
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
                d.type = lineType.QuadraticBezier;
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
                lastX=x;
                lastY=y;
                lastZ=z;

                //we can skip this when actually plotting, as we are already at Start Position.
                if (projection == 1) SetPixel(x, y, z);
                if (projection == 2) SetPixel(x, z, y);
                if (projection == 3) SetPixel(z, y, x);

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

                    if (projection == 1) SetPixel(x, y, z);
                    if (projection == 2) SetPixel(x, z, y);
                    if (projection == 3) SetPixel(z, y, x);

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

                        if (projection == 1) SetPixel(x, y, z);
                        if (projection == 2) SetPixel(x, z, y);
                        if (projection == 3) SetPixel(z, y, x);
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


        void AddLine(Int64 startX, Int64 startY, Int64 startZ, Int64 endX, Int64 endY, Int64 endZ)
        {
            Console.Write("\nCalculating Line: (");
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
            d.type = lineType.Straight;
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

            lastX=x;
            lastY=y;
            lastZ=z;

            Int64 errX = deltaMax / 2;
            Int64 errY = deltaMax / 2;
            Int64 errZ = deltaMax / 2;

            SetPixel(x, y, z);

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
                SetPixel(x, y, z);
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

        void SetPixel(Int64 x, Int64 y, Int64 z)
        {
            
            //Console.WriteLine(String.Format("({0}, {1}, {2})", x, y, z));
            if (Math.Abs(x - lastX) > 1 || Math.Abs(y - lastY) > 1|| Math.Abs(z - lastZ) > 1) {
                Console.WriteLine(String.Format("Jump Occured! ({0}, {1} , {2}),({3}, {4}, {5})", lastX, lastY, lastZ, x, y, z));
            }
            lastX = x;
            lastY = y;
            lastZ = z;

            /*
            Dot dxy = new Dot();
            dxy.layer = 0;
            dxy.size = 0.025f * z;
            dxy.color = Veldrid.RgbaFloat.Black;
            dxy.position = new Vector2(x, y);
            Data.dots.Add(dxy);
            dotIndex++;
            */            
        }
    }
}