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
[Serializable]
public class NativeGrid<T>
{
    //private GenericMatrix<T> this;
    [SerializeField, HideInInspector]
    public List<T> data;

    [SerializeField, HideInInspector]
    public UnboundedGrid Grid;

    //Extents of the grids in cell count

    [SerializeField, HideInInspector]
    public Vector3Int _gridMin;

    [SerializeField, HideInInspector]
    public Vector3Int _gridMax;

    public Vector3 WorldMin => this.Grid.GetCellCenterWorld(_gridMin);
    public Vector3 WorldMax => this.Grid.GetCellCenterWorld(_gridMax);

    [SerializeField, HideInInspector]
    public int Rows;

    [SerializeField, HideInInspector]
    public int Cols;

    //    public int Rows => _gridMax.y - _gridMin.y;
    //
    //    public int Cols => _gridMax.x - _gridMin.x;

    public Vector2Int GetNativeCoord(Vector2Int unityCoord)
        => new Vector2Int(unityCoord.y - _gridMin.y, unityCoord.x - _gridMin.x);

    public Vector2Int GetNativeCoord(Vector3 worldPosition)
    {
        Vector3Int worldToCell = this.Grid.WorldToCell(worldPosition);
        return this.GetNativeCoord((Vector2Int)worldToCell);
    }

    //From native to unity grid coordinates
    public Vector3Int GetUnityCoord(int row, int col)
        => new Vector3Int(col + _gridMin.x, row + _gridMin.y, 0);

    public Vector3 GetWorldPosition(int row, int col)
     => Grid.GetCellCenterWorld(this.GetUnityCoord(row, col));

    public NativeGrid(Grid unityGrid, Bounds bounds)

        : this(new UnboundedGrid(unityGrid), bounds)
    {
    }

    public NativeGrid(UnboundedGrid grid, Bounds bounds)
    {
        this.Grid = grid;
        _gridMin = Grid.WorldToCell(bounds.min);
        _gridMax = Grid.WorldToCell(bounds.max) + new Vector3Int(1, 1, 0);
        //j_nativeGrid = new T[Rows, Cols];
        //this = new GenericMatrix<T>(Rows, Cols);
        Rows = _gridMax.y - _gridMin.y;
        Cols = _gridMax.y - _gridMin.y;

        if (Rows <= 0 || Cols <= 0)
            throw new ArgumentOutOfRangeException("Matrix dimensions must be positive.");

        data = new List<T>(Rows * Cols);

        // Initialize the list with default values
        for (int i = 0; i < Rows * Cols; i++)
        {
            data.Add(default);
        }
    }

    public NativeGrid(NativeGrid<T> other)
    {
        DeepCopy(other);
    }

    private int GetIndex(int row, int column)
    {
        if (row < 0 || row >= Rows || column < 0 || column >= Cols)
            throw new IndexOutOfRangeException("Invalid matrix indices.");

        return row * Cols + column;
    }

    public T this[int row, int column]
    {
        get
        {
            int index = GetIndex(row, column);
            return data[index];
        }
        set
        {
            int index = GetIndex(row, column);
            data[index] = value;
        }
    }

    public void SetAll(Func<int, int, NativeGrid<T>, T> func)
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                this[row, col] = func(row, col, this);
            }
        }
    }

    //    public void SetAll(Func<int, int, T> func)
    //    {
    //        for (int row = 0; row < Rows; row++)
    //        {
    //            for (int col = 0; col < Cols; col++)
    //            {
    //                this[row, col] = func(row, col);
    //            }
    //        }
    //    }

    public void ForEach(Action<int, int> action)
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                action.Invoke(row, col);
            }
        }
    }

    public T Get(int row, int col) => this[row, col];

    public T Get(Vector2Int worldCoord)
    {
        Vector2Int nativeCoord = this.GetNativeCoord(worldCoord);
        return Get(nativeCoord.x, nativeCoord.y);
    }

    public T Set(int row, int col, T value) => this[row, col] = value;

    public bool IsInGrid(int row, int col) => row >= 0 && col >= 0
        && row < this.Rows && col < this.Cols;

    public void DeepCopy(NativeGrid<T> other)
    {
        this.Grid = new UnboundedGrid(other.Grid);
        this._gridMin = other._gridMin;
        this._gridMax = other._gridMax;
        this.Rows = other.Rows;
        this.Cols = other.Cols;
        //Perform deep copy of the other native grid
        //this.this = Copy(other.this);
        this.data = new List<T>(other.data);
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