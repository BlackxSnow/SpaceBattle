using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nito.AsyncEx;

//TODO Add movement; acceleration and braking distances
//TODO Grid abstractions - Create larger NodeGroups for low resolution pathing where possible
//TODO Smoothing 48 algorithm
//TODO Use agent bounds/size for pathing calculations

namespace Pathfinding
{
    public class PathfindingAgent : MonoBehaviour
    {
        public Vector3Int Destination = new Vector3Int();
        public bool AllowCornerCrossing;
        public Heuristics.HeuristicType Heuristic;
        public Vector3Int CurrentID { get; private set; }
        private List<Cell> RegisteredCells = new List<Cell>();

        private void Start()
        {
            CurrentID = PathfindingGrid.Instance.GetCellForLocation(transform.position).ID;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                PathTo(Destination);
            }
        }

        public void RegisterCell(Cell cell)
        {
            RegisteredCells.Add(cell);
        }
        public void DeregisterCell(Cell cell)
        {
            RegisteredCells.Remove(cell);
        }

        public class Node
        {
            public Cell cell;
            public float G;
            public float H;
            public bool Opened;
            public bool Closed;
            public Node Previous;
            public float F { get => G + H; }
        }

        const int ITERATION_LIMIT = 2500;
        Node[,,] NodeGrid;
        public void PathTo(Vector3Int destID)
        {
            Heuristics.Heuristic currentHeuristic = Heuristics.HeuristicPairs[Heuristic];
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            SortedSet<Node> nodeTree = new SortedSet<Node>(new FValueFirst());
            Vector3Int radius = PathfindingGrid.Instance.GridRadius;
            NodeGrid = new Node[radius.x * 2 + 1, radius.y * 2 + 1, radius.z * 2 + 1];
            Node startNode = new Node()
            {
                cell = PathfindingGrid.Cells[CurrentID.x, CurrentID.y, CurrentID.z],
                G = 0,
                H = 0
            };
            Node endNode = new Node()
            {
                cell = PathfindingGrid.Cells[destID.x, destID.y, destID.z],
                G = 0,
                H = 0
            };
            Vector3Int sID = startNode.cell.ID;
            Vector3Int eID = endNode.cell.ID;
            NodeGrid[sID.x, sID.y, sID.z] = startNode;
            NodeGrid[eID.x, eID.y, eID.z] = endNode;

            if (endNode.cell.IsOccupied && !RegisteredCells.Contains(endNode.cell)) return;

            nodeTree.Add(startNode);
            int iterations = 0;
            while(true)
            {
                if (iterations >= ITERATION_LIMIT)
                {
                    stopWatch.Stop();
                    Debug.LogWarning($"Path was unable to be found between {CurrentID} and {destID} after {ITERATION_LIMIT} iterations in {stopWatch.ElapsedMilliseconds}ms.");
                    break;
                }
                Node node;
                node = nodeTree.Min;
                node.Closed = true;
                if (!nodeTree.Remove(node)) throw new Exception($"{node} was not removed from tree");
                if(node == nodeTree.Min)
                {
                    throw new Exception($"Incorrect node was removed from the tree");
                }

                if (node == endNode)
                {
                    List<Node> chain = BacktraceChain(node);
                    chain.Reverse();
                    stopWatch.Stop();
                    Debug.Log($"Path found from {CurrentID} to {destID} with score {endNode.G} traversing {chain.Count} cells in {iterations} iterations taking {stopWatch.ElapsedMilliseconds}ms");
                    stopWatch.Restart();
                    List<Node> smoothedPath = SmoothPath(chain);
                    stopWatch.Stop();
                    Debug.Log($"Smoothed path found with {smoothedPath.Count} nodes, {chain.Count - smoothedPath.Count} less than the original taking {stopWatch.ElapsedMilliseconds}ms");
                    DrawPath(chain, Color.white);
                    DrawPath(smoothedPath, Color.magenta);
                    break;
                }
                List<Node> neighbours = GetNeighbours(node);
                foreach(Node neighbour in neighbours)
                {
                    if (neighbour == startNode || neighbour.Closed) continue;

                    float newg = Vector3Int.Distance(node.cell.ID, neighbour.cell.ID) + node.G;

                    if (!neighbour.Opened || newg < neighbour.G)
                    {
                        if (neighbour.Opened)
                        {
                            if (!nodeTree.Remove(neighbour)) throw new Exception($"{neighbour} was not removed from tree"); 
                        }
                        else
                        {
                            neighbour.Opened = true;
                        }
                        neighbour.G = newg;
                        neighbour.H = currentHeuristic(neighbour.cell.ID, endNode.cell.ID);
                        neighbour.Previous = node;
                        nodeTree.Add(neighbour);
                    }
                }
                iterations++;
            }
            CleanUp();
        }

        /// <summary>
        /// Smooth out a nodal path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private List<Node> SmoothPath(List<Node> path)
        {
            int nextIndex = 0;
            Vector2 agentSize = new Vector2(1, 1);
            List<Node> smoothedPath = new List<Node>();
            bool solutionFound;
            while (nextIndex != path.Count - 1)
            {
                solutionFound = false;
                Node startPos = path[nextIndex];
                for (int i = path.Count - 1; i > nextIndex; i--)
                {
                    Node endPos = path[i];
                    
                    if (RaycastCells(path[nextIndex], path[i], agentSize))
                    {
                        smoothedPath.Add(startPos);
                        smoothedPath.Add(endPos);
                        nextIndex = i;
                        solutionFound = true;
                        break;
                    }
                }
                if (!solutionFound)
                {
                    Debug.LogError("Smoothed path could not be found");
                    return path;
                }
            }
            return smoothedPath;
        }

        private List<Vector3> CurvePath(List<Vector3> path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks if a direct path from-to is valid and unoccupied.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="agentSize"></param>
        /// <returns>true if path is valid</returns>
        private bool RaycastCells(Node from, Node to, Vector2 agentSize)
        {
            Vector3Int fID = from.cell.ID;
            Vector3Int tID = to.cell.ID;
            Vector3 direction = to.cell.Position - from.cell.Position;
            Quaternion lookRot = Quaternion.LookRotation(direction, Vector3.up);
            Vector3 yAxis = lookRot * Vector3.up;
            Vector3 xAxis = lookRot * Vector3.right;
            Ray[] rays = new Ray[] {
                new Ray(from.cell.Position, direction),
                new Ray(from.cell.Position + yAxis * agentSize.y, direction),
                new Ray(from.cell.Position - yAxis * agentSize.y, direction),
                new Ray(from.cell.Position + xAxis * agentSize.x, direction),
                new Ray(from.cell.Position - xAxis * agentSize.x, direction)
            };

            //Bounding grid for ray checks
            int xmin = Mathf.Min(fID.x, tID.x);
            int xmax = Mathf.Max(fID.x, tID.x);
            int ymin = Mathf.Min(fID.y, tID.y);
            int ymax = Mathf.Max(fID.y, tID.y);
            int zmin = Mathf.Min(fID.z, tID.z);
            int zmax = Mathf.Max(fID.z, tID.z);

            for (int x = xmin; x <= xmax; x++)
            {
                for (int y = ymin; y <= ymax; y++)
                {
                    for (int z = zmin; z <= zmax; z++)
                    {
                        Cell cell = PathfindingGrid.Cells[x, y, z];
                        if (cell.IsOccupied && new Vector3Int(x,y,z) != from.cell.ID)
                        {
                            Bounds bound = new Bounds(cell.Position, PathfindingGrid.Instance.CellSize.ToVector3());
                            if(rays.Any(r => bound.IntersectRay(r)))
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        private void CleanUp()
        {
            //NodeGrid = null;
        }

        /// <summary>
        /// Follow a linked chain to the source
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private List<Node> BacktraceChain(Node node)
        {
            List<Node> chain = new List<Node>();
            chain.Add(node);
            while (node.Previous != null)
            {
                chain.Add(node.Previous);
                node = node.Previous;
            }
            return chain;
        }

        /// <summary>
        /// Return up to 26 adjacent nodes
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private List<Node> GetNeighbours(Node node)
        {
            List<Node> neighbours = new List<Node>();
            List<Node> blockedDiagonals = new List<Node>();
            for(int x = -1; x < 2; x++)
            {
                for(int y = -1; y < 2; y++)
                {
                    for(int z = -1; z < 2; z++)
                    {
                        if (x == 0 && y == 0 && z == 0) continue;
                        Vector3Int cellID = node.cell.ID;
                        int nx = cellID.x + x;
                        int ny = cellID.y + y;
                        int nz = cellID.z + z;

                        if (!IsWithinGrid(nx,ny,nz))
                        {
                            continue;
                        }

                        Cell cell = PathfindingGrid.Cells[nx, ny, nz];
                        if (cell.IsOccupied && !RegisteredCells.Contains(cell))
                        {
                            if (!AllowCornerCrossing)
                            {
                                int xInit = x == 0 ? -1 : 0;
                                for (int x1 = xInit; x1 < 2; x1 += 1)
                                {
                                    int yInit = y == 0 ? -1 : 0;
                                    for (int y1 = yInit; y1 < 2; y1 += 1)
                                    {
                                        int zInit = z == 0 ? -1 : 0;
                                        for (int z1 = zInit; z1 < 2; z1 += 1)
                                        {
                                            if (x1 == 0 && y1 == 0 && z1 == 0) continue;
                                            if (!IsWithinGrid(nx + x1, ny + y1, nz + z1)) continue;
                                            blockedDiagonals.Add(GetNode(nx + x1, ny + y1, nz + z1));
                                        }
                                    }
                                }
                            }
                            continue;
                        }

                        
                        neighbours.Add(GetNode(cell, nx, ny, nz));
                    }
                }
            }
            return neighbours.Except(blockedDiagonals).ToList();
        }

        private bool IsWithinGrid(int x, int y, int z)
        {
            if (Mathf.Clamp(x, 0, NodeGrid.GetLength(0) - 1) != x ||
                Mathf.Clamp(y, 0, NodeGrid.GetLength(1) - 1) != y ||
                Mathf.Clamp(z, 0, NodeGrid.GetLength(2) - 1) != z)
            {
                return false;
            }
            return true;
        }

        private Node GetNode(Cell cell, int x, int y, int z)
        {
            Node node;
            if (NodeGrid[x, y, z] == null)
            {
                node = new Node() { cell = cell };
                NodeGrid[x, y, z] = node;
            }
            else
            {
                node = NodeGrid[x, y, z];
            }
            return node;
        }
        private Node GetNode(int x, int y, int z)
        {
            if (!IsWithinGrid(x,y,z))
            {
                throw new Exception($"ID ({x}, {y}, {z}) is outside the bounds of the grid");
            }
            return GetNode(PathfindingGrid.Cells[x, y, z], x, y, z);
        }


        private void DrawPath(List<Node> path, Color col)
        {
            for(int i = 0; i < path.Count - 1; i++)
            {
                Debug.DrawLine(path[i].cell.Position, path[i + 1].cell.Position, col, 20f);
            }
        }

        private void DrawPath(List<Vector3> path, Color col)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Debug.DrawLine(path[i], path[i + 1], col, 20f);
            }
        }

        private void OnDrawGizmos()
        {
            if (NodeGrid != null)
            {
                foreach (Node node in NodeGrid)
                {
                    if (node == null) continue;

                    if (node.Closed)
                    {
                        Gizmos.color = new Color(0, 0, 1, 0.5f);
                    }
                    else if (node.Opened)
                    {
                        Gizmos.color = new Color(0, 0.81f, 1, 0.5f);
                    }
                    else
                    {
                        Gizmos.color = Color.grey;
                    }
                    Gizmos.DrawCube(node.cell.Position, PathfindingGrid.Instance.CellSize.ToVector3() * 0.85f);
                }
            }
        }
    }
}
