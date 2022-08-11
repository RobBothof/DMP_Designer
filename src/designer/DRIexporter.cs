using System.Numerics;

namespace Designer {

    public class DriExporter {
        public List<DrawInstruction> DrawInstructions;
        private UInt64 index = 0;
        private UInt64 dotIndex = 0;
        private int lastX = 0;
        private int lastY = 0;

        public DriExporter() {
            DrawInstructions = new List<DrawInstruction>();
        }

        public void Export(String filename) {
            index = 0;
            dotIndex = 0;
            lastX = 0;
            lastY = 0;
            foreach (Line l in Data.lines) {
                DrawInstructions.Clear();

                if (l.type == lineType.Straight) {
                    for (int i = 0; i < l.lineData.Length - 1; i++) {
                        addLine((Int32)l.lineData[i].X, (Int32)l.lineData[i].Y, (Int32)l.lineData[i + 1].X, (Int32)l.lineData[i + 1].Y);
                    }
                }

                if (l.type == lineType.QuadraticBezier) {
                    addBezier((Int32)l.lineData[0].X, (Int32)l.lineData[0].Y, (Int32)l.lineData[1].X, (Int32)l.lineData[1].Y, (Int32)l.lineData[2].X, (Int32)l.lineData[2].Y);
                }

                //save all instructions in the list

                foreach (DrawInstruction d in DrawInstructions) {
                    Console.WriteLine($"now drawing: {d.index}");
                    draw(d);
                }

            }

        }

        public void addLine(Int32 x_start, Int32 y_start, Int32 x_end, Int32 y_end) {
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

        void addBezier(Int32 x0, Int32 y0, Int32 x1, Int32 y1, Int32 x2, Int32 y2) {
            if ((x0 - x1) * (x2 - x1) > 0) {
                SplitBezierHorizontal(x0, y0, x1, y1, x2, y2);
            } else if ((y0 - y1) * (y2 - y1) > 0) {
                SplitBezierVertical(x0, y0, x1, y1, x2, y2);
            } else {
                addBezierSegment(x0, y0, x1, y1, x2, y2);
            }
        }

        void SplitBezierHorizontal(Int32 x0, Int32 y0, Int32 x1, Int32 y1, Int32 x2, Int32 y2) {
            Double t = (x0 - 2 * x1 + x2);
            t = (x0 - x1) / t;
            Int32 ysplit = (Int32)Math.Floor((1 - t) * ((1 - t) * y0 + 2.0 * t * y1) + t * t * y2 + 0.5);
            t = ((x0 * x2 - x1 * x1) * t) / (x0 - x1);
            Int32 xsplit = (Int32)Math.Floor(t + 0.5);
            Int32 ysplitc1 = (Int32)Math.Floor(((y1 - y0) * (t - x0)) / (x1 - x0) + y0 + 0.5);
            Int32 ysplitc2 = (Int32)Math.Floor(((y1 - y2) * (t - x2)) / (x1 - x2) + y2 + 0.5);

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

        void SplitBezierVertical(Int32 x0, Int32 y0, Int32 x1, Int32 y1, Int32 x2, Int32 y2) {
            Double t = y0 - 2 * y1 + y2;
            t = (y0 - y1) / t;
            Int32 xsplit = (Int32)Math.Floor((1 - t) * ((1 - t) * x0 + 2 * t * x1) + t * t * x2 + 0.5);
            t = ((y0 * y2 - y1 * y1) * t) / (y0 - y1);
            Int32 ysplit = (Int32)Math.Floor(t + 0.5);
            Int32 xsplitc1 = (Int32)Math.Floor(((x1 - x0) * (t - y0)) / (y1 - y0) + x0 + 0.5);
            Int32 xsplitc2 = (Int32)Math.Floor(((x1 - x2) * (t - y2)) / (y1 - y2) + x2 + 0.5);

            addBezierSegment(x0, y0, xsplitc1, ysplit, xsplit, ysplit);
            addBezierSegment(xsplit, ysplit, xsplitc2, ysplit, x2, y2);
        }

        void addBezierSegment(Int32 x_start, Int32 y_start, Int32 x_control, Int32 y_control, Int32 x_end, Int32 y_end) {
            //check to make sure there is no gradient change
            if (!((x_start - x_control) * (x_end - x_control) <= 0 && (y_start - y_control) * (y_end - y_control) <= 0)) {
                Console.WriteLine("Curve Changed SIGN!");
                return;
            }
            Int32 dir_x = x_start < x_end ? 1 : -1;
            Int32 dir_y = y_start < y_end ? 1 : -1;

            Int64 temp_y = (x_start - 2 * x_control + x_end);
            Int64 temp_x = (y_start - 2 * y_control + y_end);

            Int64 curve = (temp_y * (y_end - y_start) - temp_x * (x_end - x_start)) * dir_x * dir_y;

            if (curve == 0) {
                Console.WriteLine("Straight Line, no curve");
                //submit 
                addLine((int)x_start, (int)y_start, (int)x_control, (int)y_control);
                addLine((int)x_control, (int)y_control, (int) x_end, (int)y_end);
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
                addLine((int)x_start, (int)y_start, (int)x_control, (int)y_control);
                addLine((int)x_control, (int)y_control, (int)x_end, (int)y_end);
                return;
            }

            delta_x -= delta_xy;
            Int64 err = delta_x + delta_y; /* error of 1st step */
            delta_y -= delta_xy;

            DrawInstruction d = new DrawInstruction();
            d.type = lineType.QuadraticBezier;
            d.x_start = (int) x_start; d.y_start = (int) y_start; d.x_end = (int) x_end; d.y_end = (int) y_end;
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

        void draw(DrawInstruction d) {
            Int32 x = d.x_start;
            Int32 y = d.y_start;
            Int64 err = d.err;

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

            if (d.type == lineType.QuadraticBezier) {
                Int64 delta_x = d.delta_x;
                Int64 delta_y = d.delta_y;
                while (x != d.x_end && y != d.y_end) {
                    bool step_x = 2 * err - d.delta_x >= 0;
                    bool step_y = 2 * err - d.delta_y <= 0;
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

        void SetPixel(Int32 x, Int32 y) {
            if (Math.Abs(x - lastX) > 1 || Math.Abs(y - lastY) > 1) {
                Console.WriteLine(String.Format("({0}, {1})", lastX, lastY));
                Console.WriteLine(String.Format("({0}, {1})", x, y));
                Console.WriteLine("Jump Occured!");
            }

            lastX = x;
            lastY = y;

            Dot d = new Dot();
            d.layer = 0;
            d.size = 1.0f;
            d.color = Veldrid.RgbaFloat.Black;
            d.position = new Vector2(x, y);

            // Data.dots.Add(d);
            dotIndex++;
            Console.WriteLine(dotIndex);

        }

    }

}