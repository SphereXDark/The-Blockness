﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheWitnessPuzzles
{
    public class SymmetryPuzzle : Puzzle
    {
        public bool Y_Mirrored { get; }
        public Color MainColor { get; }
        public Color MirrorColor { get; }
        private readonly int MaxNodeID;
        private readonly int WidthPlus1;

        public SymmetryPuzzle(int width, int height, bool y_mirrored, Color? mainColor = null, Color? mirrorColor = null) : base(width, height)
        {
            Y_Mirrored = y_mirrored;
            MainColor = mainColor ?? Color.White;
            MirrorColor = mirrorColor ?? Color.White;
            MaxNodeID = Nodes.Max(x => x.Id);
            WidthPlus1 = Width + 1;
        }

        public IEnumerable<Node> MainSolutionNodes => base.SolutionNodes;
        public IEnumerable<Node> MirrorSolutionNodes
        {
            get
            {
                if (Y_Mirrored)
                    // Both axes mirroring
                    return MainSolutionNodes.Select(x => Nodes.First(n => n.Id == MaxNodeID - x.Id));
                else
                    // Only X-axis mirroring
                    return MainSolutionNodes.Select(x => Nodes.First(n => n.Id == (x.Id / WidthPlus1 * WidthPlus1) * 2 + Width - x.Id));
            }
        }

        public Node GetMirrorNode(Node node)
        {
            if (Y_Mirrored)
                // Both axes mirroring
                return Nodes.First(n => n.Id == MaxNodeID - node.Id);
            else
                // Only X-axis mirroring
                return Nodes.First(n => n.Id == (node.Id / WidthPlus1 * WidthPlus1) * 2 + Width - node.Id);
        }

        public override IEnumerable<Node> SolutionNodes => MainSolutionNodes.Concat(MirrorSolutionNodes);

        public IEnumerable<Edge> MainSolutionEdges => base.SolutionEdges;
        public IEnumerable<Edge> MirrorSolutionEdges => MirrorSolutionNodes.Zip(MirrorSolutionNodes.Skip(1), (idA, idB) => Edges.First(x => (idA, idB) == x));

        public override IEnumerable<Edge> SolutionEdges => MainSolutionEdges.Concat(MirrorSolutionEdges);

        protected override IEnumerable<Node> GetSolutionNodesForSectorLinesCalculation() => MainSolutionNodes;

        protected override void ModifySectorLinesBefore(List<List<Node>> sectorLines) => sectorLines.Insert(0, MirrorSolutionNodes.ToList());
        protected override void ModifySectorLinesAfter(List<List<Node>> sectorLines) => sectorLines.RemoveAt(0);

        protected override void DistributeUnusedBlocksToSectors(List<Sector> sectors, bool[,] usedBlocks)
        {
            // Get unused blocks as list
            List<Block> unusedBlocksList = new List<Block>();
            for (int x = 0; x < usedBlocks.GetLength(0); x++)
                for (int y = 0; y < usedBlocks.GetLength(1); y++)
                    if (!usedBlocks[x, y])
                        unusedBlocksList.Add(Grid[x, y]);

            List<Block> newSector = new List<Block>();

            // Create mirrored sectors of existing ones
            int maxBlockId = Width * Height - 1;
            for (int i = sectors.Count - 1; i >= 0; i--)
            {
                newSector = new List<Block>();

                foreach (Block block in sectors[i].Blocks)
                {
                    int mirrorBlockId;
                    Block mirrorBlock;

                    // XY mirror
                    if (Y_Mirrored)
                        mirrorBlockId = maxBlockId - block.Id;
                    // X mirror
                    else
                        mirrorBlockId = block.Y * Width * 2 + Width - block.Id - 1;

                    mirrorBlock = unusedBlocksList.Find(x => x.Id == mirrorBlockId);
                    // If mirrored block is alredy used, then skip whole sector
                    if (mirrorBlock == null)
                        break;
                    else
                    {
                        newSector.Add(mirrorBlock);
                        unusedBlocksList.Remove(mirrorBlock);
                    }
                }

                if (newSector.Count > 0)
                    sectors.Add(new Sector(newSector));
            }

            // All remainig unused blocks should be split into two symmetric sectors
            newSector = new List<Block>();
            int prevCount = 0;

            // Collect all connected (with edges) blocks into one sector and all remainig after that should form another sector
            if (unusedBlocksList.Count > 0)
            {
                newSector.Add(unusedBlocksList[0]);
                unusedBlocksList.RemoveAt(0);
            }
            
            while (newSector?.Count != prevCount)
            {
                prevCount = newSector.Count;
                var addition = unusedBlocksList.Where(z => newSector.SelectMany(x => x.Edges).Intersect(z.Edges).Count() > 0);
                newSector.AddRange(addition);
                unusedBlocksList.RemoveAll(x => addition.Contains(x));
            }
            if (newSector.Count > 0)
                sectors.Add(new Sector(newSector));

            if (unusedBlocksList.Count > 0)
                sectors.Add(new Sector(unusedBlocksList));
        }

        private List<List<Node>> mirrorSolutions;
        protected override List<List<int>> GetAllPossibleLines()
        {
            mirrorSolutions = new List<List<Node>>();
            return base.GetAllPossibleLines();
        }

        protected override void GAPL_AddStartNodes(List<List<Node>> solutions)
        {
            // Add mirrored starts to mirrored solutions
            base.GAPL_AddStartNodes(solutions);
            foreach (var line in solutions)
                mirrorSolutions.Add(new List<Node>() { GetMirrorNode(line[0]) });
        }

        protected override IEnumerable<Node> GAPL_GetNeighbourNodes(Node last)
        {
            Node mirNode = GetMirrorNode(last);
            // Get all neighbours of main node (this excludes nodes over broken edges)
            var main = base.GAPL_GetNeighbourNodes(last);
            // Get all neighbours of mirror node
            var mirror = base.GAPL_GetNeighbourNodes(mirNode);
            // Transform mirror neighbours to main nodes
            var mirrorMirror = mirror.Select(x => GetMirrorNode(x));
            // Return intersection
            return main.Intersect(mirrorMirror);
        }

        protected override IEnumerable<Node> GAPL_RemoveImpossibleNodes(IEnumerable<Node> possibleMoves, IEnumerable<Node> mainLine, int lineIndexInSolutions)
        {
            // Send to base func both solution lines, so main line will not cross mirrored one
            var main = base.GAPL_RemoveImpossibleNodes(possibleMoves, mainLine.Concat(mirrorSolutions[lineIndexInSolutions]), lineIndexInSolutions);
            // Also exclude nodes from center symmetry line: two lines can not enter these nodes simultaneously
            return main.Where(x => x.Id != GetMirrorNode(x).Id);
        }

        protected override void GAPL_DeleteDeadEndLine(List<List<Node>> solutions, int index)
        {
            base.GAPL_DeleteDeadEndLine(solutions, index);
            mirrorSolutions.RemoveAt(index);
        }

        protected override void GAPL_AddNewLineToAllSolutions(List<List<Node>> solutions, int index, Node node)
        {
            base.GAPL_AddNewLineToAllSolutions(solutions, index, node);
            mirrorSolutions.Add(new List<Node>(mirrorSolutions[index]) { GetMirrorNode(node) });
        }

        protected override void GAPL_ExtendLineWithNode(List<List<Node>> solutions, int index, Node node)
        {
            base.GAPL_ExtendLineWithNode(solutions, index, node);
            mirrorSolutions[index].Add(GetMirrorNode(node));
        }
    }
}
