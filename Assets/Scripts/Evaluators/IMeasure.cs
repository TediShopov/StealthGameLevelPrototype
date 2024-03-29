using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMeasure<T>
{
    void Measure(T data);
}