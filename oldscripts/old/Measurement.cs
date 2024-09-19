using System;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using System.Numerics;
using Designer;

public class Measurement : IGenerator {
    Line l;
    int stepsPerMM = 1280;
    public void Generate(int seed)
    {
        Data.lines.Clear();
        Data.dots.Clear();
        
        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*0,stepsPerMM*0,0), new Vector3(stepsPerMM*800,stepsPerMM*0,0) };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*800,stepsPerMM*0,0), new Vector3(stepsPerMM*800,stepsPerMM*1200,0) };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*800,stepsPerMM*1200,0), new Vector3(stepsPerMM*0,stepsPerMM*1200,0) };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*0,stepsPerMM*1200,0), new Vector3(stepsPerMM*0,stepsPerMM*0,0) };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*210,stepsPerMM*-0,0), new Vector3(stepsPerMM*210,stepsPerMM*297,0) };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*210,stepsPerMM*297,0), new Vector3(stepsPerMM*0,stepsPerMM*297,0) };
        Data.lines.Add(l);

        // l = new Line();
        // l.type = LineType.Straight;
        // l.acceleration = Acceleration.Single;
        // l.points = new Vector3[] { new Vector3(stepsPerMM*297,stepsPerMM*0,0), new Vector3(stepsPerMM*297,stepsPerMM*210,0) };
        // Data.lines.Add(l);

        // l = new Line();
        // l.type = LineType.Straight;
        // l.acceleration = Acceleration.Single;
        // l.points = new Vector3[] { new Vector3(stepsPerMM*297,stepsPerMM*210,0), new Vector3(stepsPerMM*0,stepsPerMM*210,0) };
        // Data.lines.Add(l);        

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*297,stepsPerMM*0,0), new Vector3(stepsPerMM*297,stepsPerMM*420,0) };
        Data.lines.Add(l); 

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*297,stepsPerMM*420,0), new Vector3(stepsPerMM*0,stepsPerMM*420,0) };
        Data.lines.Add(l);  

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*420,stepsPerMM*0,0), new Vector3(stepsPerMM*420,stepsPerMM*594,0) };
        Data.lines.Add(l); 

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*420,stepsPerMM*594,0), new Vector3(stepsPerMM*0,stepsPerMM*594,0) };
        Data.lines.Add(l);          

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*594,stepsPerMM*0,0), new Vector3(stepsPerMM*594,stepsPerMM*841,0) };
        Data.lines.Add(l); 

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*594,stepsPerMM*841,0), new Vector3(stepsPerMM*0,stepsPerMM*841,0) };
        Data.lines.Add(l);    



        // -------





        /*
        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*200,stepsPerMM*100,0), new Vector3(stepsPerMM*200,stepsPerMM*200,0) };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*200,stepsPerMM*200,0), new Vector3(stepsPerMM*100,stepsPerMM*200,0) };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*100,stepsPerMM*200,0), new Vector3(stepsPerMM*100,stepsPerMM*100,0) };
        Data.lines.Add(l);  

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*300,stepsPerMM*100,0), new Vector3(stepsPerMM*600,stepsPerMM*100,0) };
        Data.lines.Add(l);


        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*600,stepsPerMM*100,0), new Vector3(stepsPerMM*600,stepsPerMM*400,0) };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*600,stepsPerMM*400,0), new Vector3(stepsPerMM*300,stepsPerMM*400,0) };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*300,stepsPerMM*400,0), new Vector3(stepsPerMM*300,stepsPerMM*100,0) };
        Data.lines.Add(l); 

        
        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*0,stepsPerMM*0,0), new Vector3(stepsPerMM*500,stepsPerMM*0,0) };
        Data.lines.Add(l);


        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*500,stepsPerMM*0,0), new Vector3(stepsPerMM*500,stepsPerMM*500,0) };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*500,stepsPerMM*500,0), new Vector3(stepsPerMM*0,stepsPerMM*500,0) };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*0,stepsPerMM*500,0), new Vector3(stepsPerMM*0,stepsPerMM*0,0) };
        Data.lines.Add(l);  

        /*
        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*0,stepsPerMM*0,0), new Vector3(stepsPerMM*800,stepsPerMM*0,0) };
        Data.lines.Add(l);


        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*800,stepsPerMM*0,0), new Vector3(stepsPerMM*800,stepsPerMM*1200,0) };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*800,stepsPerMM*1200,0), new Vector3(stepsPerMM*0,stepsPerMM*1200,0) };
        Data.lines.Add(l);

        l = new Line();
        l.type = LineType.Straight;
        l.acceleration = Acceleration.Single;
        l.points = new Vector3[] { new Vector3(stepsPerMM*0,stepsPerMM*1200,0), new Vector3(stepsPerMM*0,stepsPerMM*0,0) };
        Data.lines.Add(l);        
        */
    }
}