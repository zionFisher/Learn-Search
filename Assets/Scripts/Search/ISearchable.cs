using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISearchable
{
    List<Vector2> Search(Vector2 start, Vector2 end, CellType[,] cells);
}
