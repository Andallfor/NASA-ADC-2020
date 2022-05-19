using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.IO;
using System.Text;

public struct CSVFILESTORAGE 
{
    // get/set used to always set them to negative
    public double lat, lon, height, slope;
    public readonly int index;
    public readonly Vector3 cartPos;

    public override string ToString() // what should be printed, for debugging purposes
    {
        return $"{index} | lat (y): {lat}, long (x): {lon}, height: {height} | slope: {slope} | cart: {cartPos}";
    }
}
public class node // base class for a point
{
        // this code is very messy, int[] should be Vector2
        // int[]/Vector2 just hold a direction
    // holds reference to a neighbor
    public Dictionary<int[], node> nearbyNodes = new Dictionary<int[], node>();

    // holds angle to specific neighbors
    public Dictionary<Vector2, float> angleToNode = new Dictionary<Vector2, float>();
    
    //---------

    public Vector3 selfPosition; // its own position, in global units

    public int[] lPos; // the node's position in local units

    public float height; // DONT USE
    // redunant, since selfPosition already contains the y
    // i just never bothered to switch up the code, oh well

    public node(Vector3 position, int[] localPos, float h) // assign values that are passed to it
    {
        selfPosition = position;
        lPos = localPos;
        height = h;
    }

    public override string ToString() // what should be printed, for debugging purposes
    {
        return $"lPos: [{lPos[0]}, {lPos[1]}], wPos: {selfPosition} | neighbors: {nearbyNodes.Count} | angles: {angleToNode.Count}";
    }
}
public class Point
{ // y is up down
    public XYZ cartPos = new XYZ(), geoPos = new XYZ(), gridPos = new XYZ(); // dont touch geo pos
    public double slope, defaultHeight, displayHeight, elevationAngle, azimuth;
    public readonly CSVFILESTORAGE csvMatrix = new CSVFILESTORAGE();
    private XYZ _dCartPos = new XYZ();
    public XYZ defaultCartPos
    {
        get {return _dCartPos;}
    }
    public int index;
    public bool fakePoint = false, inUse = false; // inUse may not be correct -> generating with speed in mind *will* cause this to fail
    public Point(XYZ cartPos = new XYZ(), XYZ geoPos = new XYZ(), XYZ gridPos = new XYZ(), CSVFILESTORAGE csvMatrix = new CSVFILESTORAGE(), double slope = 0)
    {
        this.cartPos = cartPos;
        this.geoPos = geoPos;
        this.gridPos = gridPos;
        this.csvMatrix = csvMatrix;
        this.slope = slope;
    }
    public Point(Point p)
    {
        this.cartPos = p.cartPos;
        this.geoPos = p.geoPos;
        this.gridPos = p.gridPos;
        this._dCartPos = p._dCartPos;
        this.csvMatrix = p.csvMatrix;
        this.defaultHeight = p.defaultHeight;
        this.displayHeight = p.displayHeight;
        this.slope = p.slope;
        this.index = p.index;
        this.fakePoint = p.fakePoint;
        this.inUse = p.inUse;
        this.elevationAngle = p.elevationAngle;
        this.azimuth = p.azimuth;
    }
    public void generateDataFromCSV()
    {
        this.geoPos = new XYZ(this.csvMatrix.lon, this.csvMatrix.height, this.csvMatrix.lat);
        this.defaultHeight = this.csvMatrix.height;
        this.generateCartPos();
        this.defaultHeight -= this._dCartPos.y;
        this.slope = this.csvMatrix.slope;
        this.displayHeight = defaultHeight;
    }

    public void reset()
    {
        this.cartPos = _dCartPos;
        this.gridPos = new XYZ();   
    }

    public void generateCartPos() // assumes its on moon
    {
        double radius = 1737400 + csvMatrix.height;
        double lat = this.geoPos.z * (Math.PI/(double)180);
        double lon = this.geoPos.x * (Math.PI/(double)180);

        double cosLat = Math.Cos(lat);

        this.cartPos = new XYZ(
            (radius * cosLat * Math.Cos(lon)),
            (radius * Math.Sin(lat)),
            (radius * cosLat * Math.Sin(lon))
        );

        this._dCartPos = new XYZ(this.cartPos);
    }

    public override int GetHashCode() => $"{cartPos}".Trim().GetHashCode();
    public override bool Equals(object obj)
    {
        if (!(obj is Point)) return false;

        Point p = (Point) obj;

        return p.geoPos == this.geoPos && p.cartPos == this.cartPos && p.gridPos == this.gridPos; // NOTE: only checks coords
    }
    public static bool operator ==(Point a, Point b)
    {
        if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return true;
        if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;

        return a.GetHashCode() == b.GetHashCode();
    }
    public static bool operator !=(Point a, Point b)
    {
        if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return false;
        if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return true;

        return a.GetHashCode() != b.GetHashCode();
    }
}
public struct XYZ
{
    public double x;
    public double y;
    public double z;
    public XYZ(double x = 0, double y = 0, double z = 0)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public XYZ(object obj)
    {
        this.x = 0;
        this.y = 0;
        this.z = 0;
        if (!(obj is XYZ)) return;

        XYZ pos = (XYZ) obj;
        this.x = pos.x;
        this.y = pos.y;
        this.z = pos.z;
    }

    public override string ToString() => $"{this.x}, {this.y}, {this.z}";
    public override int GetHashCode() => this.ToString().Trim().GetHashCode(); // iffy, but should work?
    public override bool Equals(object obj)
    {
        if (!(obj is XYZ)) return false;
        XYZ pos = (XYZ) obj;

        return pos.x == this.x && pos.y == this.y && pos.z == this.z;
    }
    public static bool operator ==(XYZ a, XYZ b) => a.GetHashCode() == b.GetHashCode();
    public static bool operator !=(XYZ a, XYZ b) => a.GetHashCode() != b.GetHashCode();
}
public class bounds // needs to support doubles LOWERCASE NOT UPPER
{
    public double x, y, width, height;
    public bounds(double x = 0, double y = 0, double width = 0, double height = 0)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }
    public bounds(bounds b)
    {
        this.x = b.x;
        this.y = b.y;
        this.width = b.width;
        this.height = b.height;
    }

    public override string ToString() => $"({this.x}, {this.y}) | {this.width} x {this.height}";
    public override int GetHashCode() => this.ToString().Trim().GetHashCode();
    public override bool Equals(object obj) => this.GetHashCode() == obj.GetHashCode();
    public static bool operator ==(bounds a, bounds b) => a.GetHashCode() == b.GetHashCode();
    public static bool operator !=(bounds a, bounds b) => a.GetHashCode() != b.GetHashCode();
}
public class kernel
{
    public int length;
    public int height;
    public int[,] grid;
    public double multiplyer;

    public kernel(int[,] grid, double multiplyer)
    {
        this.grid = grid;
        this.length = grid.GetLength(0);
        this.height = grid.GetLength(1);
        this.multiplyer = multiplyer;
    }

    public int getValue(int x, int y) // center is 0,0
    {
        return this.grid[x + (this.length - 1)/2, y + (this.height - 1)/2];
    }
}
