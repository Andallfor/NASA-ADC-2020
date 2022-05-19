using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.IO;
using System.Text;

/// <summary> Grid class for terrain generation. </summary>
public class GRID
{
    public Dictionary<XYZ, Point> grid = new Dictionary<XYZ, Point>();
    public List<Point> allPoints = new List<Point>();
    public bounds cartBounds = new bounds(), gridBounds = new bounds(), cartHeightBounds = new bounds();
    public XYZ offset = new XYZ();
    public Texture2D heightTexture, slopeTexture, elevationAngleTexture, azimuthAngleTexture, booleanAzimuthTexture;
    public int downFactor = 1, key = -100, gridSize;

    

    // --------------- INIT ---------------
    // All functions related to setting up a grid or copying a grid
    public GRID(GRID other) // copies the info without linking them
    {
        foreach (KeyValuePair<XYZ, Point> kvp in other.grid) this.grid.Add(kvp.Key, new Point(kvp.Value));
        foreach (Point p in other.allPoints) this.allPoints.Add(new Point(p));

        this.cartBounds = new bounds(other.cartBounds);
        this.gridBounds = new bounds(other.gridBounds);
        this.cartHeightBounds = new bounds(other.cartHeightBounds);
        this.offset = other.offset;
        this.key = other.key;
        this.downFactor = other.downFactor;
        this.heightTexture = other.heightTexture;
        this.slopeTexture = other.slopeTexture;
        this.elevationAngleTexture = other.elevationAngleTexture;
        this.azimuthAngleTexture = other.azimuthAngleTexture;
        this.gridSize = other.gridSize;
    }
    public GRID() {}
    public void overridePoints(List<Point> points)
    {
        foreach (Point p in points)
        {
            Point _p = new Point(p);
            _p.reset();
            this.allPoints.Add(_p);
        }
    }

    
    
    // --------------- UI ---------------
    // All functions that are used to show the actual mesh
    public void generatePictures(int slopeMax, int yMin, int yMax, Gradient g, Gradient g2)
    {
        Texture2D heightTexture = new Texture2D((int) this.gridBounds.width, (int) this.gridBounds.height);
        Texture2D slopeTexture = new Texture2D((int) this.gridBounds.width, (int) this.gridBounds.height);
        Texture2D azimuthTexture = new Texture2D((int) this.gridBounds.width, (int) this.gridBounds.height);
        Texture2D elevationTexture = new Texture2D((int) this.gridBounds.width, (int) this.gridBounds.height);

        foreach (KeyValuePair<XYZ, Point> kvp in this.grid) // works better with more empty spaces, will never check any empty spots
        {
            slopeTexture.SetPixel((int) kvp.Key.x, (int) kvp.Key.z, (kvp.Value.slope > slopeMax) ? Color.yellow : Color.magenta);
            heightTexture.SetPixel((int) kvp.Key.x, (int) kvp.Key.z, g.Evaluate((float) ((kvp.Value.displayHeight - yMin) / (yMax - yMin))));
            azimuthTexture.SetPixel( (int) kvp.Key.x, (int) kvp.Key.z, g2.Evaluate((((float) (kvp.Value.azimuth * Mathf.Rad2Deg) + 180f)/180f)/2f));
            elevationTexture.SetPixel((int) kvp.Key.x, (int) kvp.Key.z, g2.Evaluate(
                ((float) ((kvp.Value.elevationAngle * Mathf.Rad2Deg) - 5)) / ((float) (7 - 5))
            ));
        }

        slopeTexture.Apply();
        heightTexture.Apply();
        azimuthTexture.Apply();
        elevationTexture.Apply();

        slopeTexture.filterMode = FilterMode.Point;
        heightTexture.filterMode = FilterMode.Point;
        azimuthTexture.filterMode = FilterMode.Point;
        elevationTexture.filterMode = FilterMode.Point;

        this.heightTexture = heightTexture;
        this.slopeTexture = slopeTexture;
        this.elevationAngleTexture = elevationTexture;
        this.azimuthAngleTexture = azimuthTexture;
    }
    public float[,] generateFloatMap()
    {
        float[,] map = new float[(int) this.gridBounds.width + 1, (int) this.gridBounds.height + 1];

        int i = 0;
        int count = this.grid.Count;

        foreach (KeyValuePair<XYZ, Point> kvp in this.grid)
        {
            map[(int) kvp.Key.x, (int) kvp.Key.z] = (float) kvp.Value.displayHeight;

            master.updatePercent((float) i / (float) count);
            i++;
        }

        return map;
    }
    public Point[,] generatePointMap()
    {
        Point[,] pointMap = new Point[(int) this.gridBounds.width + 1, (int) this.gridBounds.height + 1];

        int i = 0;
        int count = this.grid.Count;

        foreach (KeyValuePair<XYZ, Point> kvp in this.grid)
        {
            pointMap[(int) kvp.Key.x, (int) kvp.Key.z] = kvp.Value;
            

            master.updatePercent((float) i / (float) count);
            i++;
        }

        return pointMap;
    }
    
    

    // --------------- MATH ---------------
    // All functions that are only used to do some calculations
    public XYZ estimateLatToGrid(XYZ pos, int df) // for the love of god please keep the grid size 1
    {
        // convert to cart
        double radius = 1737400;
        double lat = pos.z * (Math.PI/(double)180);
        double lon = pos.x * (Math.PI/(double)180);

        double cosLat = Math.Cos(lat);

        XYZ cPos = new XYZ(
            (radius * cosLat * Math.Cos(lon)) + offset.x,
            (radius * Math.Sin(lat)),
            (radius * cosLat * Math.Sin(lon)) + offset.z);

        XYZ gPos = new XYZ(
            x : reverseGridRound(gridRound(cPos.x, 1), 1),
            z : reverseGridRound(gridRound(cPos.z, 1), 1));
        
        XYZ sPos = new XYZ(
            x : Math.Floor(gPos.x / df),
            z : Math.Floor(gPos.z / df));

        return sPos;
    }
    private int reverseGridRound(double value, double step) => (int) Math.Floor(value / step);
    private double gridRound(double value, double step) => (Math.Floor(value / step) * step) + (step / (double) 2);
    public float azimuthAngle(XYZ _a)
    {
        XYZ b = new XYZ( // point given is 361000, 0, -42100 -> switch y and z, z : 0 bc z would be 0
            x : Math.Atan2(-42100_000, 361000_000),
            z : 0);
        
        XYZ a = new XYZ(
            x : _a.x * Mathf.Deg2Rad,
            z : _a.z * Mathf.Deg2Rad);

        return (float) Math.Atan2(
            Math.Sin(b.x - a.x) * Math.Cos(b.z),
            (Math.Cos(a.z) * Math.Sin(b.z)) - (Math.Sin(a.z) * Math.Cos(b.z) * Math.Cos(b.x - a.x)));
    }
    public float elevationAngle(XYZ _a, XYZ _aCart, int index) // pass lat and lon
    {
        XYZ bCart = new XYZ(361_000_000, 0, -42_100_000); // switch y and z axis

        XYZ aCart = new XYZ(_aCart.x, _aCart.z, _aCart.y);

        (double lon, double lat) a = (_a.x * Mathf.Deg2Rad, _a.z * Mathf.Deg2Rad);
        
        XYZ ab = new XYZ(bCart.x - aCart.x, bCart.y - aCart.y, bCart.z - aCart.z);
        double range = Math.Sqrt(Math.Pow(ab.x, 2) + Math.Pow(ab.y, 2) + Math.Pow(ab.z, 2));
        double rz = 
              (ab.x * Math.Cos(a.lat) * Math.Cos(a.lon)) 
            + (ab.y * Math.Cos(a.lat) * Math.Sin(a.lon)) 
            + ab.z * Math.Sin(a.lat);

        return (float) Math.Asin(rz / range);
    }




    // --------------- HELPER ---------------
    // Functions that help main functions
    public bounds forceGenerateGridBounds()
    {
        double minX = double.MaxValue;
        double maxX = double.MinValue;
        double minZ = double.MaxValue;
        double maxZ = double.MinValue;

        foreach (KeyValuePair<XYZ, Point> kvp in this.grid)
        {
            Point p = kvp.Value;
            if (!p.inUse) continue;
            minX = Math.Min(p.gridPos.x, minX);
            maxX = Math.Max(p.gridPos.x, maxX);
            minZ = Math.Min(p.gridPos.z, minZ);
            maxZ = Math.Max(p.gridPos.z, maxZ);
        }

        return new bounds(minX, minZ, maxX - minX, maxZ - minZ);
    }
    public void centerGrid()
    {
        // changes from last time: instead of squeezing all points into a set bounds
        // multiply all the points by a set amount
        // this ratio is gotten by taking the max in cartHeight bounds (since major concern is fitting height)

        int length = this.allPoints.Count;
        double maxX = double.NegativeInfinity;
        double maxZ = double.NegativeInfinity;

        double heightOffset = -this.cartHeightBounds.y;
        this.offset = new XYZ(
            x : -cartBounds.x,
            y : heightOffset,
            z : -cartBounds.y
        );

        for (int i = 0; i < length; i++)
        {
            Point p = this.allPoints[i];

            p.cartPos = new XYZ(
                x : (p.defaultCartPos.x + this.offset.x),
                y : p.defaultCartPos.y,
                z : (p.defaultCartPos.z + this.offset.z)
            );

            p.displayHeight = (p.defaultHeight + heightOffset);

            if (p.cartPos.x > maxX) maxX = p.cartPos.x;
            if (p.cartPos.z > maxZ) maxZ = p.cartPos.z;

            master.updatePercent((float) i / (float) length);
        }

        this.cartBounds = new bounds(0, 0, Math.Abs(maxX), Math.Abs(maxZ));
        this.cartHeightBounds = new bounds(0, 0, 0, this.cartHeightBounds.height + heightOffset);
    }
    public void unloadCSV(List<CSVFILESTORAGE> csvs)
    {
        this.allPoints = new List<Point>();
        int length = csvs.Count;

        double minX = double.PositiveInfinity;
        double minZ = double.PositiveInfinity;
        double maxX = double.NegativeInfinity;
        double maxZ = double.NegativeInfinity;

        for (int i = 0; i < length; i++)
        {
            Point p = new Point(csvMatrix : csvs[i]);
            p.generateDataFromCSV();
            p.index = i;
            p.elevationAngle = this.elevationAngle(new XYZ(p.geoPos), new XYZ(p.cartPos), i);
            p.azimuth = this.azimuthAngle(new XYZ(p.geoPos));
            this.allPoints.Add(p);
            if (i == 0)
            {
                this.cartHeightBounds.height = p.defaultHeight;
                this.cartHeightBounds.y = p.defaultHeight;
            }
            else
            {
                this.cartHeightBounds.height = Math.Max(this.cartHeightBounds.height, p.defaultHeight);
                this.cartHeightBounds.y = Math.Min(this.cartHeightBounds.y, p.defaultHeight);
            }

            if (p.cartPos.x < minX) minX = p.cartPos.x;
            if (p.cartPos.x > maxX) maxX = p.cartPos.x;
            if (p.cartPos.z < minZ) minZ = p.cartPos.z;
            if (p.cartPos.z > maxZ) maxZ = p.cartPos.z;

            master.updatePercent((float) i / (float) length);
        }

        this.cartBounds = new bounds(minX, minZ, Math.Abs(minX - maxX), Math.Abs(minZ - maxZ));
    }
    public GRID getPointsInGrid(XYZ start, XYZ end) // checks cart bounds, give grid bounds
    {
        int length = this.allPoints.Count;

        double yMin = double.PositiveInfinity;
        double yMax = double.NegativeInfinity;

        List<Point> returnList = new List<Point>();
        for (int i = 0; i < length; i++)
        {
            // check if point is in bounds
            XYZ pos = this.allPoints[i].cartPos;
            if (pos.x >= start.x && pos.x <= end.x && pos.z >= start.z && pos.z <= end.z)
            {
                returnList.Add(this.allPoints[i]);
                yMin = Math.Min(this.allPoints[i].defaultHeight, yMin);
                yMax = Math.Max(this.allPoints[i].defaultHeight, yMax);
            }
        }

        GRID g = new GRID();
        g.overridePoints(returnList);
        g.cartBounds = new bounds(start.x, start.z, end.x - start.x, end.z - start.z);
        g.cartHeightBounds = new bounds(0, yMin, 0, yMax);
        g.downFactor = this.downFactor;
        return g;
    }
    public GRID getPointsInGridFromGridPos(XYZ start, XYZ end)
    {
        int length = this.allPoints.Count;

        double yMin = double.PositiveInfinity;
        double yMax = double.NegativeInfinity;

        List<Point> returnList = new List<Point>();
        for (int i = 0; i < length; i++)
        {
            // check if point is in bounds
            XYZ pos = this.allPoints[i].gridPos;
            if (pos.x >= start.x && pos.x <= end.x && pos.z >= start.z && pos.z <= end.z)
            {
                returnList.Add(this.allPoints[i]);
                yMin = Math.Min(this.allPoints[i].defaultHeight, yMin);
                yMax = Math.Max(this.allPoints[i].defaultHeight, yMax);
            }
        }

        GRID g = new GRID();
        g.overridePoints(returnList);
        g.cartBounds = g.generateCartBounds();
        Debug.Log(g.cartBounds);
        g.cartHeightBounds = new bounds(0, yMin, 0, yMax);
        g.downFactor = this.downFactor;
        return g;
    }
    public bounds generateCartBounds()
    {
        int length = this.allPoints.Count;

        double minX = double.PositiveInfinity;
        double minZ = double.PositiveInfinity;
        double maxX = double.NegativeInfinity;
        double maxZ = double.NegativeInfinity;

        for (int i = 0; i < length; i++)
        {
            Point p = this.allPoints[i];

            this.cartHeightBounds.height = Math.Max(this.cartHeightBounds.height, p.defaultHeight);
            this.cartHeightBounds.y = Math.Min(this.cartHeightBounds.y, p.defaultHeight);

            if (p.cartPos.x < minX) minX = p.cartPos.x;
            if (p.cartPos.x > maxX) maxX = p.cartPos.x;
            if (p.cartPos.z < minZ) minZ = p.cartPos.z;
            if (p.cartPos.z > maxZ) maxZ = p.cartPos.z;
        }

        return new bounds(minX, minZ, Math.Abs(minX - maxX), Math.Abs(minZ - maxZ));
    }
    private int emptySpace() => (int) ((this.gridBounds.width * this.gridBounds.height) - this.grid.Count);



    // --------------- MAIN ---------------
    // Main functions
    public void scaleTo(int maxSize, int steps, kernel k, bool optimize = false, bool overrideSafety = false)
    {
        if (Math.Min(this.gridBounds.width, this.gridBounds.height) < maxSize) return;

        // make sure no holes are generated
        int bestSize = (int) Mathf.Sqrt(Mathf.Min((float) this.gridBounds.width, (float) this.gridBounds.height));
        if (bestSize < maxSize && this.grid.Count < 160_000) maxSize = bestSize;

        this.downFactor = (int) Math.Ceiling(Math.Max(this.gridBounds.width, this.gridBounds.height) / (double) maxSize);

        if (optimize) this.efficentPool(downFactor);
        else
        {
            this.pool(downFactor);
            this.grid = this.convolute(this.grid, k).Item1;
        }

        this.gridBounds = new bounds(0, 0, Math.Round(this.gridBounds.width / downFactor, MidpointRounding.AwayFromZero), Math.Round(this.gridBounds.height / downFactor, MidpointRounding.AwayFromZero));
        this.cartHeightBounds.y /= (double) downFactor;
        this.cartHeightBounds.height /= (double) downFactor;
    }
    private void pool(int size) // it is linked, thats fine
    {
        Dictionary<XYZ, Point> returnGrid = new Dictionary<XYZ, Point>();

        int i = 0;
        int count = this.grid.Count;

        double maxHeight = double.MinValue;

        foreach (KeyValuePair<XYZ, Point> kvp in this.grid) // maybe set all points to false?
        {
            XYZ pos = kvp.Key;
            Point p = kvp.Value;

            XYZ smallerGridPos = new XYZ(
                x : Math.Floor(pos.x / (double) size),
                z : Math.Floor(pos.z / (double) size)
            );

            p.inUse = false;
            p.gridPos = smallerGridPos;
            if (p.displayHeight > maxHeight) maxHeight = (p.displayHeight);
            p.displayHeight /= (double) size;

            if (!returnGrid.ContainsKey(smallerGridPos)) 
            {
                returnGrid.Add(new XYZ(smallerGridPos), p);
                p.inUse = true;
            }
            else if (p.defaultHeight > returnGrid[smallerGridPos].defaultHeight)
            {
                returnGrid[smallerGridPos].inUse = false;
                returnGrid[smallerGridPos] = p;
                p.inUse = true;
            }

            master.updatePercent((float) i / (float) count);
            i++;
        }
        this.grid = returnGrid;
    }
    private void efficentPool(int size)
    {
        Dictionary<XYZ, Point> returnGrid = new Dictionary<XYZ, Point>();

        foreach (KeyValuePair<XYZ, Point> kvp in this.grid) // maybe set all points to false?
        {
            XYZ pos = kvp.Key;
            Point p = kvp.Value;

            XYZ smallerGridPos = new XYZ(
                x : Math.Floor(pos.x / (double) size),
                z : Math.Floor(pos.z / (double) size)
            );

            p.displayHeight /= (double) size;
            p.inUse = false;
            p.gridPos = smallerGridPos;
            
            returnGrid[smallerGridPos] = p;
        }

        this.grid = returnGrid;
    }
    private (Dictionary<XYZ, Point> grid, int successCode) convolute(Dictionary<XYZ, Point> dict, kernel k)
    {
        Dictionary<XYZ, Point> returnGrid = new Dictionary<XYZ, Point>();

        foreach (KeyValuePair<XYZ, Point> kvp in dict)
        {
            double value = 0;
            int amount = 0;

            for (int column = (k.length - 1)/2; column < (k.length - 1)/2; column++)
            {
                for (int row = (k.height - 1)/2; row < (k.height - 1)/2; row++)
                {
                    XYZ checkPos = new XYZ(x : kvp.Key.x + column, z : kvp.Key.z + row);
                    if (dict.ContainsKey(checkPos)) 
                    {
                        value += (double) k.getValue((int) column, (int) row) * dict[checkPos].displayHeight;
                        amount++;
                    }
                }
            }

            if (amount >= 1) // if the amount of real points nearby is equal to the total points around it
            {
                value *= k.multiplyer;
                kvp.Value.displayHeight = value;
            }
            
            returnGrid.Add(kvp.Key, kvp.Value);
        }

        return (returnGrid, 0);
    }
    public void generateGrid(int gridSize, bool optimize = true)
    {
        this.gridSize = gridSize;
        if (!optimize) gGrid(gridSize);
        else efficientGGrid(gridSize);
    }
    private void gGrid(int gridSize)
    {
        this.grid = new Dictionary<XYZ, Point>();
        Dictionary<XYZ, Point> emptyGrid = new Dictionary<XYZ, Point>();
        double minX = double.PositiveInfinity, minZ = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxZ = double.NegativeInfinity;

        int i = 0;
        int count = this.allPoints.Count;

        foreach (Point p in this.allPoints)
        {
            XYZ pos = new XYZ(
                x : this.reverseGridRound(this.gridRound(p.cartPos.x, gridSize), gridSize),
                z : this.reverseGridRound(this.gridRound(p.cartPos.z, gridSize), gridSize)
            );

            p.displayHeight = this.reverseGridRound(this.gridRound(p.displayHeight, gridSize), gridSize);

            // apply this to height

            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.z < minZ) minZ = pos.z;
            if (pos.z > maxZ) maxZ = pos.z;

            p.gridPos = pos;
            p.inUse = false;

            if (emptyGrid.ContainsKey(pos))
            {
                if (emptyGrid[pos].defaultHeight < p.defaultHeight)
                {
                    emptyGrid[pos].inUse = false;
                    emptyGrid[pos] = p;
                    p.inUse = true;
                } 
            }
            else 
            {
                emptyGrid.Add(pos, p);
                p.inUse = true;
            }

            master.updatePercent((float) i / (float) count);
            i++;
        }
        this.grid = emptyGrid;

        this.gridBounds = new bounds(minX, minZ, Math.Abs(minX - maxX), Math.Abs(minZ - maxZ));
    }
    private void efficientGGrid(int gridSize)
    {
        this.grid = new Dictionary<XYZ, Point>();
        Dictionary<XYZ, Point> emptyGrid = new Dictionary<XYZ, Point>();
        double minX = double.PositiveInfinity, minZ = double.PositiveInfinity;
        double maxX = double.NegativeInfinity, maxZ = double.NegativeInfinity;

        int i = 0;
        int count = this.allPoints.Count;

        foreach (Point p in this.allPoints)
        {
            XYZ pos = new XYZ(
                x : this.reverseGridRound(this.gridRound(p.cartPos.x, gridSize), gridSize),
                z : this.reverseGridRound(this.gridRound(p.cartPos.z, gridSize), gridSize)
            );

            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.z < minZ) minZ = pos.z;
            if (pos.z > maxZ) maxZ = pos.z;

            p.gridPos = pos;
            p.inUse = false;

            emptyGrid[pos] = p;
            if (i == 68900)
            {
                Debug.Log("Gridding");
                Debug.Log(p.defaultCartPos);
                Debug.Log(p.cartPos);
                Debug.Log(p.displayHeight);
                Debug.Log(p.defaultHeight);
                Debug.Log(p.geoPos);
                Debug.Log(p.gridPos);
                Debug.Log("-------");
            }
        }
        this.grid = emptyGrid;

        this.gridBounds = new bounds(minX, minZ, Math.Abs(minX - maxX), Math.Abs(minZ - maxZ));

        master.updatePercent((float) i / (float) count);
        i++;
        
    }



    // --------------- OVERRIDES ---------------
    // Any overriden default functions
    public override string ToString()
    {
        return 
         $@"Grid {this.key} : {this.GetHashCode()}
--------
Total points: {this.allPoints.Count}
Gridded points: {this.grid.Count}
Down factor: {this.downFactor}
Offset: {this.offset}
--------
Cart bounds (x,z): {this.cartBounds}
Cart bounds (y): {this.cartHeightBounds}
Grid bounds: {this.gridBounds}
            ";
    }
}
