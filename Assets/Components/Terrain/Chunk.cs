using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Antymology.Terrain
{
    /// <summary>
    /// A Chunk is a unity-instanced object which holds the underlying world data in segments
    /// </summary>
    public class Chunk : MonoBehaviour
    {

        #region Fields

        /// <summary>
        /// The X Coordinate of theis chunk.
        /// </summary>
        public int x { get; set; }

        /// <summary>
        /// The y Coordinate of this chunk.
        /// </summary>
        public int y { get; set; }

        /// <summary>
        /// The z coordinate of this chunk.
        /// </summary>
        public int z { get; set; }

        /// <summary>
        /// Whether or not this chunk needs to have its mesh updated
        /// </summary>
        public bool updateNeeded { get; set; }

        /// <summary>
        /// The Unity mesh component of this chunk object.
        /// </summary>
        Mesh mesh;

        /// <summary>
        /// The Unity mesh renderer component of this chunk object.
        /// </summary>
        MeshRenderer renderer;

        /// <summary>
        /// The collider of this mesh
        /// </summary>
        MeshCollider collider;

        #endregion

        #region Methods

        /// <summary>
        /// initialize the gameobject to have a mesh and mesh renderer, and set the references internally.
        /// </summary>
        public void Init(Material mat)
        {
            mesh = gameObject.AddComponent<MeshFilter>().mesh;
            renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.material = mat;
            collider = gameObject.AddComponent<MeshCollider>();
        }

        /// <summary>
        /// Gets the Block with local coordinates from this chunk.
        /// </summary>
        /// <param name="localXCoordinate">The local x coordinate of the desired block.</param>
        /// <param name="localYCoordinate">The local y coordinate of the desired block.</param>
        /// <param name="localZCoordinate">The local z coordinate of the desired block.</param>
        /// <exception cref="System.Exception">Invalid local coordinate (either less than 0, or greater than the chunk diameter.</exception>
        /// <returns>The Block with local coordinates from this chunk.</returns>
        public AbstractBlock GetBlock(int localXCoordinate, int localYCoordinate, int localZCoordinate)
        {
            return WorldManager.Instance.GetBlock(localXCoordinate + x, localYCoordinate + y, localZCoordinate + z);
        }

        /// <summary>
        /// Generates a mesh object which is then passed to the Mesh component of this monobehaviour.
        /// </summary>
        public void GenerateMesh()
        {
            // Creation of temporary mesh variables which we will use to create the unity mesh object.
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            int NumFaces = 0;

            // For each block in this chunk
            for (int x = 0; x < ConfigurationManager.Instance.Chunk_Diameter; x++)
                for (int z = 0; z < ConfigurationManager.Instance.Chunk_Diameter; z++)
                    for (int y = 0; y < ConfigurationManager.Instance.Chunk_Diameter; y++)
                    {
                        // If the block is visible, display all faces which are adjacent to invisible blocks
                        AbstractBlock ours = GetBlock(x, y, z);
                        if (ours.isVisible())
                        {
                                if (!GetBlock(x + 1, y, z).isVisible())
                                    AddPosXFace(x, y, z, ours, vertices, triangles, uvs, ref NumFaces);
                                if (!GetBlock(x - 1, y, z).isVisible())
                                    AddNegXFace(x, y, z, ours, vertices, triangles, uvs, ref NumFaces);
                                if (!GetBlock(x, y + 1, z).isVisible())
                                    AddPosYFace(x, y, z, ours, vertices, triangles, uvs, ref NumFaces);
                                if (!GetBlock(x, y - 1, z).isVisible())
                                    AddNegYFace(x, y, z, ours, vertices, triangles, uvs, ref NumFaces);
                                if (!GetBlock(x, y, z + 1).isVisible())
                                    AddPosZFace(x, y, z, ours, vertices, triangles, uvs, ref NumFaces);
                                if (!GetBlock(x, y, z - 1).isVisible())
                                    AddNegZFace(x, y, z, ours, vertices, triangles, uvs, ref NumFaces);
                        }
                    }

            // Clear whatever data was in the mesh previously
            mesh.Clear();

            // Add in newly calculated values
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();

            // Optimize, and normal calculation
            MeshUtility.Optimize(mesh);
            mesh.RecalculateNormals();
            collider.sharedMesh = mesh;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Adds the 6 most recent triangles to the triangle list. This number here is predetermined by the order in which the vertices are added in the calling code.
        /// </summary>
        /// <param name="triangles">The list containing the current triangles.</param>
        /// <param name="numFaces">The reference to the integer holding the current number of faces.</param>
        void AddRecent6Triangles(List<int> triangles, ref int numFaces)
        {
            triangles.Add(numFaces * 4);
            triangles.Add(numFaces * 4 + 1);
            triangles.Add(numFaces * 4 + 2);
            triangles.Add(numFaces * 4);
            triangles.Add(numFaces * 4 + 2);
            triangles.Add(numFaces * 4 + 3);
            numFaces++;
        }

        /// <summary>
        /// Adds the texture of the most recently added face to the list of uvs.
        /// </summary>
        /// <param name="blockType">The block type which contains the desired texture coordinates.</param>
        /// <param name="uvs">The list of UVs.</param>
        void AddRecentFaceTexture(AbstractBlock blockType, List<Vector2> uvs)
        {
            Vector2 tileMapCoordinate = blockType.tileMapCoordinate();
            uvs.Add(
                new Vector2(
                    ConfigurationManager.Instance.Tile_Map_Unit_Ratio * tileMapCoordinate.x + ConfigurationManager.Instance.Tile_Map_Unit_Ratio,
                    ConfigurationManager.Instance.Tile_Map_Unit_Ratio * tileMapCoordinate.y
                )
            );
            uvs.Add(
                new Vector2(
                    ConfigurationManager.Instance.Tile_Map_Unit_Ratio * tileMapCoordinate.x + ConfigurationManager.Instance.Tile_Map_Unit_Ratio,
                    ConfigurationManager.Instance.Tile_Map_Unit_Ratio * tileMapCoordinate.y + ConfigurationManager.Instance.Tile_Map_Unit_Ratio
                )
            );
            uvs.Add(
                new Vector2(
                    ConfigurationManager.Instance.Tile_Map_Unit_Ratio * tileMapCoordinate.x,
                    ConfigurationManager.Instance.Tile_Map_Unit_Ratio * tileMapCoordinate.y + ConfigurationManager.Instance.Tile_Map_Unit_Ratio
                )
            );
            uvs.Add(
                new Vector2(
                    ConfigurationManager.Instance.Tile_Map_Unit_Ratio * tileMapCoordinate.x,
                    ConfigurationManager.Instance.Tile_Map_Unit_Ratio * tileMapCoordinate.y
                )
            );
        }

        /// <summary>
        /// Adds the positive y face to the vertex list, triangle list, and uv list.
        /// </summary>
        /// <param name="x">The local x coordinate of this face.</param>
        /// <param name="y">The local y coordinate of this face.</param>
        /// <param name="z">The local z coordinate of this face.</param>
        /// <param name="block">The reference to the block for grabbing type and tile map coordinates.</param>
        /// <param name="vertices">The list of vertices to update.</param>
        /// <param name="triangles">The list of triangles to update.</param>
        /// <param name="uvs">The list of uvs to update.</param>
        /// <param name="numFaces">The number of faces currently added. Used for counting triangles.</param>
        void AddPosYFace(int x, int y, int z, AbstractBlock block, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, ref int numFaces)
        {
            vertices.Add(new Vector3(x, y, z + 1));
            vertices.Add(new Vector3(x + 1, y, z + 1));
            vertices.Add(new Vector3(x + 1, y, z));
            vertices.Add(new Vector3(x, y, z));
            AddRecent6Triangles(triangles, ref numFaces);
            AddRecentFaceTexture(block, uvs);
        }

        /// <summary>
        /// Adds the negative y face to the vertex list, triangle list, and uv list.
        /// </summary>
        /// <param name="x">The local x coordinate of this face.</param>
        /// <param name="y">The local y coordinate of this face.</param>
        /// <param name="z">The local z coordinate of this face.</param>
        /// <param name="block">The reference to the block for grabbing type and tile map coordinates.</param>
        /// <param name="vertices">The list of vertices to update.</param>
        /// <param name="triangles">The list of triangles to update.</param>
        /// <param name="uvs">The list of uvs to update.</param>
        /// <param name="numFaces">The number of faces currently added. Used for counting triangles.</param>
        void AddNegYFace(int x, int y, int z, AbstractBlock block, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, ref int numFaces)
        {
            vertices.Add(new Vector3(x, y - 1, z));
            vertices.Add(new Vector3(x + 1, y - 1, z));
            vertices.Add(new Vector3(x + 1, y - 1, z + 1));
            vertices.Add(new Vector3(x, y - 1, z + 1));
            AddRecent6Triangles(triangles, ref numFaces);
            AddRecentFaceTexture(block, uvs);
        }

        /// <summary>
        /// Adds the positive z face to the vertex list, triangle list, and uv list.
        /// </summary>
        /// <param name="x">The local x coordinate of this face.</param>
        /// <param name="y">The local y coordinate of this face.</param>
        /// <param name="z">The local z coordinate of this face.</param>
        /// <param name="block">The reference to the block for grabbing type and tile map coordinates.</param>
        /// <param name="vertices">The list of vertices to update.</param>
        /// <param name="triangles">The list of triangles to update.</param>
        /// <param name="uvs">The list of uvs to update.</param>
        /// <param name="numFaces">The number of faces currently added. Used for counting triangles.</param>
        void AddPosZFace(int x, int y, int z, AbstractBlock block, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, ref int numFaces)
        {
            vertices.Add(new Vector3(x + 1, y - 1, z + 1));
            vertices.Add(new Vector3(x + 1, y, z + 1));
            vertices.Add(new Vector3(x, y, z + 1));
            vertices.Add(new Vector3(x, y - 1, z + 1));
            AddRecent6Triangles(triangles, ref numFaces);
            AddRecentFaceTexture(block, uvs);
        }

        /// <summary>
        /// Adds the positive x face to the vertex list, triangle list, and uv list.
        /// </summary>
        /// <param name="x">The local x coordinate of this face.</param>
        /// <param name="y">The local y coordinate of this face.</param>
        /// <param name="z">The local z coordinate of this face.</param>
        /// <param name="block">The reference to the block for grabbing type and tile map coordinates.</param>
        /// <param name="vertices">The list of vertices to update.</param>
        /// <param name="triangles">The list of triangles to update.</param>
        /// <param name="uvs">The list of uvs to update.</param>
        /// <param name="numFaces">The number of faces currently added. Used for counting triangles.</param>
        void AddPosXFace(int x, int y, int z, AbstractBlock block, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, ref int numFaces)
        {
            vertices.Add(new Vector3(x + 1, y - 1, z));
            vertices.Add(new Vector3(x + 1, y, z));
            vertices.Add(new Vector3(x + 1, y, z + 1));
            vertices.Add(new Vector3(x + 1, y - 1, z + 1));
            AddRecent6Triangles(triangles, ref numFaces);
            AddRecentFaceTexture(block, uvs);
        }

        /// <summary>
        /// Adds the negative z face to the vertex list, triangle list, and uv list.
        /// </summary>
        /// <param name="x">The local x coordinate of this face.</param>
        /// <param name="y">The local y coordinate of this face.</param>
        /// <param name="z">The local z coordinate of this face.</param>
        /// <param name="block">The reference to the block for grabbing type and tile map coordinates.</param>
        /// <param name="vertices">The list of vertices to update.</param>
        /// <param name="triangles">The list of triangles to update.</param>
        /// <param name="uvs">The list of uvs to update.</param>
        /// <param name="numFaces">The number of faces currently added. Used for counting triangles.</param>
        void AddNegZFace(int x, int y, int z, AbstractBlock block, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, ref int numFaces)
        {
            vertices.Add(new Vector3(x, y - 1, z));
            vertices.Add(new Vector3(x, y, z));
            vertices.Add(new Vector3(x + 1, y, z));
            vertices.Add(new Vector3(x + 1, y - 1, z));
            AddRecent6Triangles(triangles, ref numFaces);
            AddRecentFaceTexture(block, uvs);
        }

        /// <summary>
        /// Adds the negative x face to the vertex list, triangle list, and uv list.
        /// </summary>
        /// <param name="x">The local x coordinate of this face.</param>
        /// <param name="y">The local y coordinate of this face.</param>
        /// <param name="z">The local z coordinate of this face.</param>
        /// <param name="block">The reference to the block for grabbing type and tile map coordinates.</param>
        /// <param name="vertices">The list of vertices to update.</param>
        /// <param name="triangles">The list of triangles to update.</param>
        /// <param name="uvs">The list of uvs to update.</param>
        /// <param name="numFaces">The number of faces currently added. Used for counting triangles.</param>
        void AddNegXFace(int x, int y, int z, AbstractBlock block, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, ref int numFaces)
        {
            vertices.Add(new Vector3(x, y - 1, z + 1));
            vertices.Add(new Vector3(x, y, z + 1));
            vertices.Add(new Vector3(x, y, z));
            vertices.Add(new Vector3(x, y - 1, z));
            AddRecent6Triangles(triangles, ref numFaces);
            AddRecentFaceTexture(block, uvs);
        }

        #endregion

        #region Update

        /// <summary>
        /// Late update happens after all default updates have been called.
        /// </summary>
        public void LateUpdate()
        {
            // If we need to update ou mesh, then do so now.
            if (updateNeeded)
            {
                GenerateMesh();
                updateNeeded = false;
            }
        }

        #endregion

    }
}