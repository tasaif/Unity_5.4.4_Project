using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mod
{

    public class DynamicTexture
    {
        [JsonIgnore] public Texture2D static_texture;
        public string texture_data;
        public int w, h;

        public void initialize(Texture2D _static_texture)
        {
            static_texture = _static_texture;
            w = static_texture.width;
            h = static_texture.height;
            texture_data = Convert.ToBase64String(static_texture.EncodeToPNG());
        }
    }

    public class DynamicMaterial
    {
        [JsonIgnore] public Material static_material;

        public string texture_name;
        public List<float> colors;
        public string shader_name;

        public void initialize(Material _static_material)
        {
            static_material = _static_material;
            colors = new List<float>() { static_material.color.r, static_material.color.g, static_material.color.b, static_material.color.a };
            shader_name = static_material.shader.name;
            if (static_material.mainTexture != null)
            {
                texture_name = static_material.mainTexture.name;
            }
        }
    }

    public class DynamicSubmesh
    {
        public int[] indeces;
        public MeshTopology meshTopology;

        public void initialize(int[] _indeces, MeshTopology _meshTopology)
        {
            indeces = _indeces;
            meshTopology = _meshTopology;
        }
    }

    public class DynamicMesh
    {
        [JsonIgnore] public MeshRenderer static_mesh_renderer;
        [JsonIgnore] public MeshFilter static_mesh_filter;
        [JsonIgnore] public Mesh mesh;
        public List<string> vertices;
        public List<string> normals;
        public List<string> uvs;
        public List<string> uvs2;
        public List<int> triangles;
        public int subMeshCount;
        public List<DynamicSubmesh> subMeshMetadatas;

        public void initialize(Mesh _mesh)
        {
            vertices = new List<string>();
            normals = new List<string>();
            uvs = new List<string>();
            uvs2 = new List<string>();
            triangles = new List<int>();
            mesh = _mesh;
            foreach (var vertex in mesh.vertices) vertices.Add(vertex.ToString("F4"));
            foreach (var normal in mesh.normals) normals.Add(normal.ToString("F4"));
            foreach (var uv in mesh.uv) uvs.Add(uv.ToString("F4"));
            foreach (var uv2 in mesh.uv2) uvs2.Add(uv2.ToString("F4"));
            foreach (var triangle in mesh.triangles) triangles.Add(triangle);
            subMeshMetadatas = new List<DynamicSubmesh>();
            subMeshCount = mesh.subMeshCount;
            for(int i=0; i<subMeshCount; i++)
            {
                var subMeshMetadata = new DynamicSubmesh();
                subMeshMetadata.initialize(mesh.GetIndices(i), mesh.GetTopology(i));
                subMeshMetadatas.Add(subMeshMetadata);
            }
        }

        public Mesh GenerateMesh()
        {
            List<Vector3> new_vertices = new List<Vector3>();
            List<Vector3> new_normals = new List<Vector3>();
            List<Vector2> new_uvs = new List<Vector2>();
            List<Vector2> new_uvs2 = new List<Vector2>();
            Mesh mesh = new Mesh();

            foreach (var vertex in vertices) new_vertices.Add(Mod.Helper.ParseVector3(vertex));
            foreach (var normal in normals) new_normals.Add(Mod.Helper.ParseVector3(normal));
            foreach (var uv in uvs) new_uvs.Add(Mod.Helper.ParseVector2(uv));
            foreach (var uv2 in uvs2) new_uvs2.Add(Mod.Helper.ParseVector2(uv2));

            mesh.SetVertices(new_vertices);
            mesh.SetNormals(new_normals);
            mesh.uv = new_uvs.ToArray();
            mesh.uv2 = new_uvs2.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.subMeshCount = subMeshCount;
            for(int i=0; i<subMeshCount; i++) {
                var subMeshMetadata = subMeshMetadatas[i];
                mesh.SetIndices(subMeshMetadata.indeces, subMeshMetadata.meshTopology, i); 
            }

            mesh.RecalculateBounds();
            mesh.Optimize();
            return mesh;
        }
    }

    public class DynamicGameObject
    {

        [JsonIgnore] public GameObject static_gameobject;

        public string name;
        public string mesh_name;
        public List<string> material_names;
        public int parent_instance_id = 0;
        public int instance_id = 0;
        public string position;
        public string localScale;
        public string localEulerAngles;
        public bool has_mesh_collider;

        public void initialize(GameObject _static_gameobject, DynamicBundle dynamicBundle)
        {
            static_gameobject = _static_gameobject;
            if (static_gameobject.transform.parent != null)
            {
                parent_instance_id = static_gameobject.transform.parent.GetInstanceID();
            }
            instance_id = static_gameobject.transform.GetInstanceID();
            name = static_gameobject.name;
            position = static_gameobject.transform.position.ToString("F4");
            localScale = static_gameobject.transform.localScale.ToString("F4");
            localEulerAngles = static_gameobject.transform.localEulerAngles.ToString("F4");
            has_mesh_collider = static_gameobject.GetComponent<MeshCollider>() != null;
            MeshFilter mf = static_gameobject.GetComponent<MeshFilter>();
            MeshRenderer mr = static_gameobject.GetComponent<MeshRenderer>();
            material_names = new List<string>();
            if (mf != null)
            {
                mesh_name = mf.sharedMesh.name;
                if (!dynamicBundle.meshes_table.ContainsKey(mesh_name))
                {
                    DynamicMesh dynamicMesh = new DynamicMesh();
                    dynamicMesh.initialize(mf.sharedMesh);
                    dynamicMesh.static_mesh_filter = mf;
                    dynamicMesh.static_mesh_renderer = mr;
                    dynamicBundle.meshes_table.Add(mesh_name, dynamicMesh);
                }
            }
            if (mr != null) {
                foreach(var material in mr.sharedMaterials)
                {
                    material_names.Add(material.name);
                    if (!dynamicBundle.materials_table.ContainsKey(material.name))
                    {
                        DynamicMaterial dynamicMaterial = new DynamicMaterial();
                        dynamicMaterial.initialize(material);
                        dynamicBundle.materials_table.Add(material.name, dynamicMaterial);
                        Texture2D texture = (Texture2D)material.mainTexture;
                        if (texture != null)
                        {
                            if (!dynamicBundle.textures_table.ContainsKey(texture.name))
                            {
                                DynamicTexture dynamicTexture = new DynamicTexture();
                                dynamicTexture.initialize(texture);
                                dynamicBundle.textures_table.Add(texture.name, dynamicTexture);
                            }
                        }
                    }
                }
            }
        }
    }

    public class DynamicBundle
    {
        public Dictionary<string, DynamicMaterial> materials_table;
        public Dictionary<string, DynamicTexture> textures_table;
        public Dictionary<string, DynamicMesh> meshes_table;
        public Dictionary<int, DynamicGameObject> gameobjects_table;

        [JsonIgnore] GameObject root;

        public void SerializeInitialization()
        {
            List<GameObject> traversal_queue = new List<GameObject>() { root };
            while (traversal_queue.Count > 0)
            {
                GameObject g = traversal_queue[0];
                DynamicGameObject dgo = new DynamicGameObject();
                dgo.initialize(g, this);
                traversal_queue.RemoveAt(0);
                gameobjects_table.Add(dgo.instance_id, dgo);
                foreach (Transform child in g.transform)
                {
                    traversal_queue.Add(child.gameObject);
                }
            }
        }

        /*
         * Loads an object from the scene to get ready for converting to JSON
         */
        public void initialize(GameObject _root)
        {
            gameobjects_table = new Dictionary<int, DynamicGameObject>();
            meshes_table = new Dictionary<string, DynamicMesh>();
            textures_table = new Dictionary<string, DynamicTexture>();
            materials_table = new Dictionary<string, DynamicMaterial>();
            root = _root;
            SerializeInitialization();
        }

        public void Load()
        {
            // Create Meshes
            foreach(string name in meshes_table.Keys)
            {
                DynamicMesh mesh = meshes_table[name];
                Mesh _mesh = mesh.GenerateMesh();
                _mesh.name = name;
                mesh.mesh = _mesh;
            }

            // Create Textures
            foreach(string name in textures_table.Keys)
            {
                DynamicTexture texture = textures_table[name];
                texture.static_texture = new Texture2D(texture.w, texture.h);
                texture.static_texture.name = name;
                if (texture.texture_data != null) texture.static_texture.LoadImage(Convert.FromBase64String(texture.texture_data));
                else
                {
                    Mod.Settings.Log("Missing texture data for " + name);
                }
                texture.static_texture.Apply();
            }

            // Create Materials
            foreach(string name in materials_table.Keys)
            {
                DynamicMaterial material = materials_table[name];
                material.static_material = new Material(Shader.Find(material.shader_name));
                material.static_material.name = name;
                material.static_material.color = new Color(material.colors[0], material.colors[1], material.colors[2], material.colors[3]);
                if (material.texture_name != null) {
                    material.static_material.mainTexture = textures_table[material.texture_name].static_texture;
                 }
            }

            // Create GameObjects
            foreach(int instance_id in gameobjects_table.Keys)
            {
                var gameobject = gameobjects_table[instance_id];
                gameobject.static_gameobject = new GameObject(gameobject.name);
                if (gameobject.mesh_name != null)
                {
                    var mf = gameobject.static_gameobject.AddComponent<MeshFilter>();
                    var mr = gameobject.static_gameobject.AddComponent<MeshRenderer>();
                    try
                    {
                        mf.mesh = meshes_table[gameobject.mesh_name].mesh;
                    }
                    catch
                    {
                        Debug.Log("Failed to load mesh for " + gameobject.name);
                    }
                    List<Material> materials = new List<Material>();
                    foreach (var material_name in gameobject.material_names)
                    {
                        materials.Add(materials_table[material_name].static_material);
                    }
                    mr.materials = materials.ToArray();
                    if (gameobject.has_mesh_collider)
                    {
                        gameobject.static_gameobject.AddComponent<MeshCollider>();
                    }
                }
            }
            // Configure Hierarchy
            foreach (int instance_id in gameobjects_table.Keys)
            {
                var gameobject = gameobjects_table[instance_id];
                if (gameobject.parent_instance_id != 0)
                {
                    gameobject.static_gameobject.transform.parent = gameobjects_table[gameobject.parent_instance_id].static_gameobject.transform;
                }
            }
            // Set Transformations
            foreach (int instance_id in gameobjects_table.Keys)
            {
                var gameobject = gameobjects_table[instance_id];
                gameobject.static_gameobject.transform.position = Mod.Helper.ParseVector3(gameobject.position);
                gameobject.static_gameobject.transform.localScale = Mod.Helper.ParseVector3(gameobject.localScale);
                gameobject.static_gameobject.transform.localEulerAngles = Mod.Helper.ParseVector3(gameobject.localEulerAngles);
            }
        }
    }

}

