using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Given a unity grid and bounds construct a native representation with a generic methods
/// Used for discretisizing
/// !WARNING! Grid is assumed to be a the center
/// </summary>
public class NativeGrid<T>
{
    private T[,] _nativeGrid;

    //public Grid Grid;
    public UnboundedGrid Grid;

    //Extents of the grids in cell count
    public Vector3Int _gridMin { get; set; }

    public Vector3Int _gridMax { get; set; }

    public Vector3 WorldMin => this.Grid.GetCellCenterWorld(_gridMin);
    public Vector3 WorldMax => this.Grid.GetCellCenterWorld(_gridMax);

    public int GetRows() => _gridMax.y - _gridMin.y;

    public int GetCols() => _gridMax.x - _gridMin.x;

    public Vector2Int GetNativeCoord(Vector2Int unityCoord)
        => new Vector2Int(unityCoord.y - _gridMin.y, unityCoord.x - _gridMin.x);

    //From native to unity grid coordinates
    public Vector3Int GetUnityCoord(int row, int col)
        => new Vector3Int(col + _gridMin.x, row + _gridMin.y, 0);

    public Vector3 GetWorldPosition(int row, int col)
     => Grid.GetCellCenterWorld(this.GetUnityCoord(row, col));

    public NativeGrid(Grid unityGrid, Bounds bounds)
    {
        //this.Grid = unityGrid;
        this.Grid = new UnboundedGrid(unityGrid);
        _gridMin = Grid.WorldToCell(bounds.min);
        _gridMax = Grid.WorldToCell(bounds.max) + new Vector3Int(1, 1, 0);
        _nativeGrid = new T[GetRows(), GetCols()];
    }

    public NativeGrid(UnboundedGrid grid, Bounds bounds)
    {
        this.Grid = grid;
        _gridMin = Grid.WorldToCell(bounds.min);
        _gridMax = Grid.WorldToCell(bounds.max) + new Vector3Int(1, 1, 0);
        _nativeGrid = new T[GetRows(), GetCols()];
    }

    public NativeGrid(NativeGrid<T> other)
    {
        DeepCopy(other);
    }

    public void SetAll(Func<int, int, NativeGrid<T>, T> func)
    {
        for (int row = 0; row < GetRows(); row++)
        {
            for (int col = 0; col < GetCols(); col++)
            {
                _nativeGrid[row, col] = func(row, col, this);
            }
        }
    }

    public void ForEach(Action<int, int> action)
    {
        for (int row = 0; row < GetRows(); row++)
        {
            for (int col = 0; col < GetCols(); col++)
            {
                action.Invoke(row, col);
            }
        }
    }

    public T Get(int row, int col) => _nativeGrid[row, col];

    public T Set(int row, int col, T value) => _nativeGrid[row, col] = value;

    public bool IsInGrid(int row, int col) => row >= 0 && col >= 0
        && row < _nativeGrid.GetLength(0) && col < _nativeGrid.GetLength(1);

    public void DeepCopy(NativeGrid<T> other)
    {
        this.Grid = other.Grid;
        this._gridMin = other._gridMin;
        this._gridMax = other._gridMax;
        //Perform deep copy of the other native grid
        this._nativeGrid = Copy(other._nativeGrid);
    }

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