using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Given a unity grid and bounds construct a native representation with a generic methods
/// Used for discretisizing
/// !WARNING! Grid is assumed to be a the center 
/// </summary>
public class NativeGrid<T>  
{
    private T[,] _nativeGrid;
    public Grid Grid;

    //Extents of the grids in cell count
    Vector3Int _gridMin;
    Vector3Int _gridMax;
    public int GetRows() => _gridMax.y - _gridMin.y;
    public int GetCols() => _gridMax.x - _gridMin.x;

    //From native to unity grid coordinates
    public Vector3Int GetUnityCoord(int row, int col) 
        => new Vector3Int(col + _gridMin.x, row + _gridMin.y, 0);
    public Vector3 GetWorldPosition(int row, int col) 
     =>  Grid.GetCellCenterWorld(this.GetUnityCoord(row,col));
    
    public NativeGrid(Grid unityGrid, Bounds bounds)
    {
        this.Grid = unityGrid;
        _gridMin = Grid.WorldToCell(bounds.min);
        _gridMax = Grid.WorldToCell(bounds.max);
        _nativeGrid = new T[GetRows(), GetCols()];
    }
    public void SetAll(Func<int,int,NativeGrid<T>,T> func) 
    {
        for (int row = 0; row < GetRows(); row++)
        {
            for (int col = 0; col < GetCols(); col++)
            {
                _nativeGrid[row,col] = func(row, col,this);
            }
        }
    }
    public void ForEach(Action<int,int> action) 
    {
        for (int row = 0; row < GetRows(); row++)
        {
            for (int col = 0; col < GetCols(); col++)
            {
                action.Invoke(row, col);
            }
        }
    }
    public T Get(int row, int col)=> _nativeGrid[row,col];
    public T Set(int row, int col,T value)=> _nativeGrid[row,col] = value;
    public bool IsInGrid(int row, int col) => row >= 0 && col >= 0 
        && row < _nativeGrid.GetLength(0) && col < _nativeGrid.GetLength(1);
    
    public static T[,] Copy<T>(T[,] array)
    {
        int width = array.GetLength(0);
        int height = array.GetLength(1);
        T[,] copy = new T[width, height];

        for (int w = 0; w < width; w++)
        {
            for (int h = 0; h < height; h++)
            {
                copy[w, h] = array[w, h];
            }
        }

        return copy;
    }


}
