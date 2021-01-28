using System.Collections;
using System.Collections.Generic;
using UnityAsync;
using UnityEngine;
using System.Linq;
using UnityEngine.Assertions.Must;

namespace Pathfinding
{
    public class PathfindingGrid : MonoBehaviour
    {
        public static PathfindingGrid Instance { get; private set; }

        public Vector3Int GridRadius = new Vector3Int(15, 0, 15);
        public float CellSize = 5;
        public Vector3 GridCenter = new Vector3(0, 0, 0);

        public static Cell[,,] Cells;

        private void Start()
        {
            if (!Instance)
            {
                Instance = this;
            }
            else
            {
                Destroy(this);
            }

            Cells = new Cell[GridRadius.x * 2 + 1, GridRadius.y * 2 + 1, GridRadius.z * 2 + 1];

            Vector3Int CenterID = new Vector3Int(GridRadius.x, GridRadius.y, GridRadius.z);

            for (int x = 0; x < Cells.GetLength(0); x++)
            {
                for (int y = 0; y < Cells.GetLength(1); y++)
                {
                    for (int z = 0; z < Cells.GetLength(2); z++)
                    {
                        Vector3 pos = new Vector3
                        {
                            x = (x - CenterID.x) * CellSize,
                            y = (y - CenterID.y) * CellSize,
                            z = (z - CenterID.z) * CellSize
                        };
                        Cells[x, y, z] = new Cell(pos, new Vector3Int(x, y, z));
                    }
                }
            }

            UpdateOccupiedCells();
        }

        public Cell GetCellForLocation(Vector3 pos)
        {
            if (!GetGridBounds().Contains(pos))
            {
                throw new System.Exception($"Grid does not contain position {pos}");
            }

            Vector3 adjustedPos = pos - GridCenter;
            Vector3Int cellID = new Vector3Int()
            {
                x = Mathf.RoundToInt(adjustedPos.x / CellSize) + GridRadius.x,
                y = Mathf.RoundToInt(adjustedPos.y / CellSize) + GridRadius.y,
                z = Mathf.RoundToInt(adjustedPos.z / CellSize) + GridRadius.z
            };
            return Cells[cellID.x, cellID.y, cellID.z];
        }

        public Bounds GetGridBounds()
        {
            Vector3 size = new Vector3();
            for (int i = 0; i < 3; i++)
            {
                size[i] = (GridRadius[i] + 0.5f) * CellSize;
            }
            return new Bounds(GridCenter, size);
        }
        public Bounds GetGridIDBounds()
        {
            return new Bounds(new Vector3(GridRadius.x, GridRadius.y, GridRadius.z), new Vector3(GridRadius.x * 2 + 1, GridRadius.y * 2 + 1, GridRadius.z * 2 + 1));
        }


        private async void UpdateOccupiedCells()
        {
            while (true)
            {
                await Await.Seconds(1.0f);
                (from Cell cell in Cells where cell.IsOccupied select cell).ToList().ForEach(c => c.UpdateStatus());
            }
        }

        //private void OnDrawGizmos()
        //{
        //    if (Cells != null)
        //    {
        //        foreach (Cell cell in Cells)
        //        {
        //            Gizmos.color = cell.IsOccupied ? Color.red : Color.green;
        //            Gizmos.DrawWireCube(cell.Position, CellSize.ToVector3() * 0.9f);
        //        }
        //    }
        //}
    } 
}
