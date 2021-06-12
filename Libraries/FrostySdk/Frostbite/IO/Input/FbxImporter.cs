﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using FrostbiteSdk;
using Frosty;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;
//using UsefulThings;
using Vortice.Mathematics;

namespace FrostySdk.Frostbite.IO.Input
{

	public class FBXImporter
	{
		public MeshSet meshSet { get; set; }

		private Stream resStream;

		private ResAssetEntry resEntry;

		private MeshImportSettings settings;

		public float Scale
		{
			get;
			set;
		} = 1f;


		public int XAxis
		{
			get;
			set;
		}

		public int YAxis
		{
			get;
			set;
		} = 1;


		public int ZAxis
		{
			get;
			set;
		} = 2;


		public float FlipZ
		{
			get;
			set;
		} = 1f;

		private AssetManager assetManager => AssetManager.Instance;

		public FBXImporter()
		{
		}

		public void ImportFBX(string filename, MeshSet inMeshSet, EbxAsset asset, EbxAssetEntry entry, MeshImportSettings inSettings)
		{
			ulong resRid = ((dynamic)asset.RootObject).MeshSetResource;
			//resEntry = assetManager.GetResEntry(resRid);
			resEntry = assetManager.GetResEntry(entry.Name);
			settings = inSettings;
			meshSet = inMeshSet;
			resStream = assetManager.GetRes(resEntry);
			if (meshSet.Type != 0 && meshSet.Type != MeshType.MeshType_Skinned)
			{
				throw new Exception("Wrong Original EBX Type");
			}
            entry.LinkedAssets.Clear();
            resEntry.LinkedAssets.Clear();
            using (FbxManager fbxManager = new FbxManager())
			{
				FbxIOSettings iOSettings = new FbxIOSettings(fbxManager, "IOSRoot");
				fbxManager.SetIOSettings(iOSettings);
				FbxScene fbxScene = new FbxScene(fbxManager, "");
				LoadScene(fbxManager, fbxScene, filename);
				List<FbxNode>[] lodArray = new List<FbxNode>[7];
				int lodsCount = 0;
				foreach (FbxNode child in fbxScene.RootNode.Children)
				{
					string text = child.Name;
					if (!text.Contains("lod", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					int colonIndex = text.IndexOf(':');
					if (colonIndex != -1)
					{
						if (int.TryParse(text[colonIndex - 1].ToString(), out var result))
						{
							if (lodArray[result] == null)
							{
								lodArray[result] = new List<FbxNode>();
								lodsCount++;
							}
							lodArray[result].Add(child);
						}
						continue;
					}
					text = text.Substring(text.Length - 1);
					if (int.TryParse(text, out var result2))
					{
						if (lodArray[result2] == null)
						{
							lodArray[result2] = new List<FbxNode>();
							lodsCount++;
						}
						lodArray[result2].AddRange(child.Children);
					}
				}
				if (lodsCount == 0)
				{
					throw new Exception("Number of LOD is 0!");
				}
				if (lodsCount < meshSet.Lods.Count)
				{
					Debug.WriteLine($"Mesh only has {lodsCount} LOD(s), and should have {meshSet.Lods.Count}. The missing LODs will be auto-generated from the lowest-quality LOD.");

					List<FbxNode> lowestQualityLod = lodArray[lodsCount - 1];
					for (; lodsCount < meshSet.Lods.Count; lodsCount++)
					{
						lodArray[lodsCount] = lowestQualityLod;
					}


				}
				for (int i = 0; i < meshSet.Lods.Count; i++)
				{
					ProcessLod(lodArray[i], i);
				}
			}
			meshSet.FullName = resEntry.Name;
			byte[] resData = meshSet.ToBytes();
            //assetManager.ModifyRes(resRid, resData, meshSet.Meta);
            assetManager.ModifyRes(resEntry.Name, resData, meshSet.Meta);
            entry.LinkAsset(resEntry);


			((dynamic)asset.RootObject).ComputeGraph = default(PointerRef);
			AssetManager.Instance.ModifyEbx(entry.Name, asset);
		}

		private float CubeMapFaceID(float inX, float inY, float inZ)
		{
			if (Math.Abs(inZ) >= Math.Abs(inX) && Math.Abs(inZ) >= Math.Abs(inY))
			{
				if (!(inZ < 0f))
				{
					return 4f;
				}
				return 5f;
			}
			if (Math.Abs(inY) >= Math.Abs(inX))
			{
				if (!(inY < 0f))
				{
					return 2f;
				}
				return 3f;
			}
			if (!(inX < 0f))
			{
				return 0f;
			}
			return 1f;
		}

		private int FindGreatestComponent(Quaternion quat)
		{
			int result = (int)(CubeMapFaceID(quat.X, quat.Y, quat.Z) * 0.5f);
			if (!(Math.Abs(quat.W) > Math.Max(Math.Abs(quat.X), Math.Max(Math.Abs(quat.Y), Math.Abs(quat.Z)))))
			{
				return result;
			}
			return 3;
		}

		private void LoadScene(FbxManager manager, FbxScene scene, string filename)
		{
			if (manager == null)
			{
				throw new ArgumentNullException("manager");
			}
			if (scene == null)
			{
				throw new ArgumentNullException("scene");
			}
			if (filename == null)
			{
				throw new ArgumentNullException("filename");
			}
			FbxManager.GetFileFormatVersion(out var pMajor, out var pMinor, out var pRevision);
			using FrostbiteSdk.Import.FbxImporter fbxImporter = new FrostbiteSdk.Import.FbxImporter(manager, "");
			if (!fbxImporter.Initialize(filename, -1, manager.GetIOSettings()))
			{
				//Log.Error(fbxImporter.Status.ErrorString);
				throw new Exception(fbxImporter.Status.ErrorString);
				//return;
			}
			fbxImporter.GetFileVersion(out pRevision, out pMinor, out pMajor);
			if (fbxImporter.IsFBX() && !fbxImporter.Import(scene))
			{
				throw new Exception(fbxImporter.Status.ErrorString);
				//Log.Error(fbxImporter.Status.ErrorString);
			}
		}
		/*
		private void ProcessLod(List<FbxNode> nodes, int lodIndex)
		{
			if (nodes == null)
			{
				throw new ArgumentNullException("nodes");
			}
			MeshSetLod meshSetLod = meshSet.Lods[lodIndex];
			List<FbxNode> list = new List<FbxNode>();
			foreach (FbxNode node in nodes)
			{
				if (node.GetNodeAttribute(FbxNodeAttribute.EType.eMesh) != null)
				{
					list.Add(node);
				}
			}
			if (list.Count == 0)
			{
				throw new Exception("No meshes found");
			}
			List<MeshSetSection> list5 = new List<MeshSetSection>();
			List<MeshSetSection> list6 = new List<MeshSetSection>();
			List<MeshSetSection> list7 = new List<MeshSetSection>();
			foreach (MeshSetSection section in meshSetLod.Sections)
			{
				if (section.Name != "")
				{
					list5.Add(section);
				}
				else if (meshSetLod.IsSectionInCategory(section, MeshSubsetCategory.MeshSubsetCategory_ZOnly))
				{
					list6.Add(section);
				}
				else
				{
					list7.Add(section);
				}
			}
			List<FbxNode> list8 = new List<FbxNode>();
			List<FbxNode> list9 = new List<FbxNode>();
			List<FbxNode> list10 = new List<FbxNode>();
			list8.AddRange(new FbxNode[list5.Count]);
			foreach (FbxNode item in list)
			{
				string sectionName = item.Name;
				if (sectionName.Contains(':'))
				{
					sectionName = sectionName.Remove(0, sectionName.IndexOf(':') + 1);
				}
				int num = list5.FindIndex((MeshSetSection a) => a.Name == sectionName);
				if (num != -1 && list8[num] == null)
				{
					list8[num] = item;
				}
				else
				{
					list9.Add(item);
				}
			}
			List<byte[]> list11 = new List<byte[]>();
			List<List<uint>> list12 = new List<List<uint>>();
			uint num2 = 0u;
			uint startIndex = 0u;
			meshSetLod.ClearBones();
			for (int i = 0; i < list8.Count; i++)
			{
				FbxNode fbxNode = list8[i];
				int sectionIndex = meshSetLod.Sections.IndexOf(list5[i]);
				if (fbxNode == null)
				{
					if (list9.Count == 0)
					{
						list5[i].VertexCount = 0u;
						list5[i].PrimitiveCount = 0u;
						list5[i].VertexOffset = 0u;
						list5[i].StartIndex = 0u;
						continue;
					}
					fbxNode = list9[0];
					list9.RemoveAt(0);
				}
				MemoryStream memoryStream = new MemoryStream();
				List<uint> list2 = new List<uint>();
				ProcessSection(new FbxNode[1]
				{
					fbxNode
				}, meshSetLod, sectionIndex, memoryStream, list2, num2, ref startIndex);
				list11.Add(memoryStream.ToArray());
				list12.Add(list2);
				num2 += (uint)(int)memoryStream.Length;
				memoryStream.Dispose();
				list10.Add(fbxNode);
			}
			for (int j = 0; j < list6.Count; j++)
			{
				int sectionIndex2 = meshSetLod.Sections.IndexOf(list6[j]);
				if (j == 0)
				{
					MemoryStream memoryStream2 = new MemoryStream();
					List<uint> list3 = new List<uint>();
					ProcessSection(list10.ToArray(), meshSetLod, sectionIndex2, memoryStream2, list3, num2, ref startIndex);
					list11.Add(memoryStream2.ToArray());
					list12.Add(list3);
					num2 += (uint)(int)memoryStream2.Length;
					memoryStream2.Dispose();
				}
				else
				{
					list6[j].VertexCount = 0u;
					list6[j].PrimitiveCount = 0u;
					list6[j].VertexOffset = 0u;
					list6[j].StartIndex = 0u;
				}
			}
			for (int k = 0; k < list7.Count; k++)
			{
				int sectionIndex3 = meshSetLod.Sections.IndexOf(list7[k]);
				if (k == 0)
				{
					MemoryStream memoryStream3 = new MemoryStream();
					List<uint> list4 = new List<uint>();
					ProcessSection(list10.ToArray(), meshSetLod, sectionIndex3, memoryStream3, list4, num2, ref startIndex);
					list11.Add(memoryStream3.ToArray());
					list12.Add(list4);
					num2 += (uint)(int)memoryStream3.Length;
					memoryStream3.Dispose();
				}
				else
				{
					list7[k].VertexCount = 0u;
					list7[k].PrimitiveCount = 0u;
					list7[k].VertexOffset = 0u;
					list7[k].StartIndex = 0u;
				}
			}
			bool flag = false;
			foreach (MeshSetSection section2 in meshSetLod.Sections)
			{
				if (section2.VertexCount > 65535)
				{
					flag = true;
					break;
				}
			}
			using NativeWriter nativeWriter = new NativeWriter(new MemoryStream());
			foreach (byte[] item2 in list11)
			{
				nativeWriter.WriteBytes(item2);
			}
			nativeWriter.WritePadding(16);
			meshSetLod.VertexBufferSize = (uint)nativeWriter.BaseStream.Position;
			foreach (List<uint> item4 in list12)
			{
				foreach (uint item3 in item4)
				{
					if (flag)
					{
						nativeWriter.WriteUInt32LittleEndian(item3);
					}
					else
					{
						nativeWriter.Write((ushort)item3);
					}
				}
			}
			meshSetLod.IndexBufferSize = (uint)(nativeWriter.BaseStream.Position - meshSetLod.VertexBufferSize);
			nativeWriter.WritePadding(16);
			meshSetLod.SetIndexBufferFormatSize(flag ? 4 : 2);
			if (meshSetLod.ChunkId != Guid.Empty)
			{
				assetManager.ModifyChunk(meshSetLod.ChunkId, ((MemoryStream)nativeWriter.BaseStream).ToArray());
				ChunkAssetEntry chunkEntry = assetManager.GetChunkEntry(meshSetLod.ChunkId);
				resEntry.LinkAsset(chunkEntry);
			}
			else
			{
				meshSetLod.SetInlineData(((MemoryStream)nativeWriter.BaseStream).ToArray());
			}
		}

		*/
		private void ProcessLod(List<FbxNode> nodes, int lodIndex)
		{
			MeshSetLod meshSetLod = meshSet.Lods[lodIndex];
			List<FbxNode> list = new List<FbxNode>();
			foreach (FbxNode node in nodes)
			{
				if (node.GetNodeAttribute(FbxNodeAttribute.EType.eMesh) != null)
				{
					list.Add(node);
				}
			}
			if (list.Count == 0)
			{
				throw new Exception("No Nodes!!");
				//throw new FBXImportNoMeshesFoundException(lodIndex);
			}
			List<MeshSetSection> list2 = new List<MeshSetSection>();
			List<MeshSetSection> list3 = new List<MeshSetSection>();
			List<MeshSetSection> list4 = new List<MeshSetSection>();
			foreach (MeshSetSection section in meshSetLod.Sections)
			{
				if (section.Name != "")
				{
					list2.Add(section);
				}
				else if (meshSetLod.IsSectionInCategory(section, MeshSubsetCategory.MeshSubsetCategory_ZOnly))
				{
					list3.Add(section);
				}
				else
				{
					list4.Add(section);
				}
			}
			List<FbxNode> list5 = new List<FbxNode>();
			List<FbxNode> list6 = new List<FbxNode>();
			List<FbxNode> list7 = new List<FbxNode>();
			list5.AddRange(new FbxNode[list2.Count]);
			foreach (FbxNode item in list)
			{
				string sectionName = item.Name;
				if (sectionName.Contains(':'))
				{
					sectionName = sectionName.Remove(0, sectionName.IndexOf(':') + 1);
				}
				int num = list2.FindIndex((MeshSetSection a) => a.Name == sectionName);
				if (num != -1 && list5[num] == null)
				{
					list5[num] = item;
				}
				else
				{
					list6.Add(item);
				}
			}
			List<byte[]> lstSectionBytes = new List<byte[]>();
			List<List<uint>> list9 = new List<List<uint>>();
			uint num2 = 0u;
			uint startIndex = 0u;
			meshSetLod.ClearBones();
			for (int i = 0; i < list5.Count; i++)
			{
				FbxNode fbxNode = list5[i];
				int sectionIndex = meshSetLod.Sections.IndexOf(list2[i]);
				if (fbxNode == null)
				{
					if (list6.Count <= 0)
					{
						list2[i].VertexCount = 0u;
						list2[i].PrimitiveCount = 0u;
						list2[i].VertexOffset = 0u;
						list2[i].StartIndex = 0u;
						continue;
					}
					fbxNode = list6.First();
					list6.RemoveAt(0);
				}
				MemoryStream memoryStream = new MemoryStream();
				List<uint> list10 = new List<uint>();
				ProcessSection(new FbxNode[1]
				{
			fbxNode
				}, meshSetLod, sectionIndex, memoryStream, list10, num2, ref startIndex);
				lstSectionBytes.Add(memoryStream.ToArray());
				list9.Add(list10);
				num2 += (uint)(int)memoryStream.Length;
				memoryStream.Dispose();
				list7.Add(fbxNode);
			}
			for (int j = 0; j < list3.Count; j++)
			{
				int sectionIndex2 = meshSetLod.Sections.IndexOf(list3[j]);
				if (j == 0)
				{
					MemoryStream memoryStream2 = new MemoryStream();
					List<uint> list11 = new List<uint>();
					ProcessSection(list7.ToArray(), meshSetLod, sectionIndex2, memoryStream2, list11, num2, ref startIndex);
					lstSectionBytes.Add(memoryStream2.ToArray());
					list9.Add(list11);
					num2 += (uint)(int)memoryStream2.Length;
					memoryStream2.Dispose();
				}
				else
				{
					list3[j].VertexCount = 0u;
					list3[j].PrimitiveCount = 0u;
					list3[j].VertexOffset = 0u;
					list3[j].StartIndex = 0u;
				}
			}
			for (int k = 0; k < list4.Count; k++)
			{
				int sectionIndex3 = meshSetLod.Sections.IndexOf(list4[k]);
				if (k == 0)
				{
					MemoryStream memoryStream3 = new MemoryStream();
					List<uint> list12 = new List<uint>();
					ProcessSection(list7.ToArray(), meshSetLod, sectionIndex3, memoryStream3, list12, num2, ref startIndex);
					lstSectionBytes.Add(memoryStream3.ToArray());
					list9.Add(list12);
					num2 += (uint)(int)memoryStream3.Length;
					memoryStream3.Dispose();
				}
				else
				{
					list4[k].VertexCount = 0u;
					list4[k].PrimitiveCount = 0u;
					list4[k].VertexOffset = 0u;
					list4[k].StartIndex = 0u;
				}
			}

			bool flag = false;
			foreach (MeshSetSection section2 in meshSetLod.Sections)
			{
				if (section2.VertexCount > 65535)
				{
					flag = true;
					break;
				}
			}
			using NativeWriter nativeWriter = new NativeWriter(new MemoryStream());
			foreach (byte[] sectionBytes in lstSectionBytes)
			{
				nativeWriter.Write(sectionBytes);
			}
			nativeWriter.WritePadding(16);
			meshSetLod.VertexBufferSize = (uint)nativeWriter.BaseStream.Position;
			foreach (List<uint> item3 in list9)
			{
				foreach (uint item4 in item3)
				{
					if (flag)
					{
						nativeWriter.Write(item4);
					}
					else
					{
						nativeWriter.Write((ushort)item4);
					}
				}
			}
			nativeWriter.WritePadding(16);
			meshSetLod.IndexBufferSize = (uint)(nativeWriter.BaseStream.Position - meshSetLod.VertexBufferSize);
			meshSetLod.SetIndexBufferFormatSize(flag ? 4 : 2);



            // --------------------------------------
            // Modifying the chunk fails --- >>
            if (meshSetLod.ChunkId != Guid.Empty)
            {
                AssetManager.Instance.ModifyChunk(meshSetLod.ChunkId, ((MemoryStream)nativeWriter.BaseStream).ToArray());
                ChunkAssetEntry chunkEntry = AssetManager.Instance.GetChunkEntry(meshSetLod.ChunkId);
                resEntry.LinkAsset(chunkEntry);
            }
            else
            {
                meshSetLod.SetInlineData(((MemoryStream)nativeWriter.BaseStream).ToArray());
            }
        }
		/*
		private unsafe void ProcessSection(FbxNode[] sectionNodes, MeshSetLod meshLod, int sectionIndex, MemoryStream verticesBuffer, List<uint> indicesBuffer, uint vertexOffset, ref uint startIndex)
		{
			MeshSetSection meshSetSection = meshLod.Sections[sectionIndex];
			uint num = 0u;
			meshSetSection.VertexOffset = vertexOffset;
			meshSetSection.StartIndex = startIndex;
			meshSetSection.VertexCount = 0u;
			meshSetSection.PrimitiveCount = 0u;
			List<ushort> list = new List<ushort>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			dynamic skeletonRootObject = null;
			if (settings != null && settings.SkeletonAsset != "")
			{
				skeletonRootObject = AssetManager.Instance.GetEbx(AssetManager.Instance.GetEbxEntry(settings.SkeletonAsset)).RootObject;
			}
			FbxNode[] array = sectionNodes;
			foreach (FbxNode fbxNode in array)
			{
				if (fbxNode == null)
				{
					continue;
				}
				FbxMesh fbxMesh = new FbxMesh(fbxNode.GetNodeAttribute(FbxNodeAttribute.EType.eMesh));
				FbxSkin fbxSkin = ((fbxMesh.GetDeformerCount(FbxDeformer.EDeformerType.eSkin) != 0) ? new FbxSkin(fbxMesh.GetDeformer(0, FbxDeformer.EDeformerType.eSkin)) : null);
				if (!(fbxSkin != null))
				{
					continue;
				}
				foreach (FbxCluster cluster in fbxSkin.Clusters)
				{
					FbxNode link = cluster.GetLink();
					if (link.Name.StartsWith("PROC") && !list3.Contains(link.Name))
					{
						list3.Add(link.Name);
					}
				}
				if (ProfilesLibrary.DataVersion == 20180807
					|| ProfilesLibrary.DataVersion == 20160927
					|| ProfilesLibrary.DataVersion == 2017092
					|| ProfilesLibrary.DataVersion == 20190729
					|| ProfilesLibrary.IsMadden20DataVersion()
					|| ProfilesLibrary.IsMadden21DataVersion()
					)
				{
					for (int j = 0; j < skeletonRootObject.BoneNames.Count; j++)
					{
						if (!list.Contains((ushort)j))
						{
							list.Add((ushort)j);
							list2.Add(skeletonRootObject.BoneNames[j]);
						}
					}
					continue;
				}
				if (ProfilesLibrary.DataVersion == 20160607
					|| ProfilesLibrary.DataVersion == 20161021
					|| ProfilesLibrary.DataVersion == 20171117 
					|| ProfilesLibrary.DataVersion == 20180914 
					|| ProfilesLibrary.DataVersion == 20190911
					|| ProfilesLibrary.IsFIFA21DataVersion()

					)
				{
					for (int k = 0; k < skeletonRootObject.BoneNames.Count; k++)
					{
						if (!list.Contains((ushort)k))
						{
							list.Add((ushort)k);
							list2.Add(skeletonRootObject.BoneNames[k]);
						}
					}
					foreach (string item in list3)
					{
						ushort num2 = ushort.Parse(item.Replace("PROC_Bone", ""));
						for (int l = 0; l <= num2; l++)
						{
							if (!list.Contains((ushort)(skeletonRootObject.BoneNames.Count + l)))
							{
								list.Add((ushort)(skeletonRootObject.BoneNames.Count + l));
								list2.Add("PROC_Bone" + l);
							}
						}
					}
					continue;
				}
				foreach (FbxCluster cluster2 in fbxSkin.Clusters)
				{
					if (cluster2.ControlPointIndicesCount != 0)
					{
						FbxNode link2 = cluster2.GetLink();
						ushort num3 = (ushort)skeletonRootObject.BoneNames.IndexOf(link2.Name);
						if (!list.Contains(num3))
						{
							list.Add(num3);
							list2.Add(skeletonRootObject.BoneNames[num3]);
						}
					}
				}
			}
			ushort[] array2 = list.ToArray();
			string[] array3 = list2.ToArray();
			Array.Sort(array2, array3);
			meshSetSection.SetBones(array2);
			meshLod.AddBones(array2, array3);
			list.Clear();
			list.AddRange(array2);
			array = sectionNodes;
			Vector4 val3 = default(Vector4);
			Vector3 val4 = default(Vector3);
			Vector3 val5 = default(Vector3);
			Vector3 val6 = default(Vector3);
			Vector4 val13 = default(Vector4);
			foreach (FbxNode fbxNode2 in array)
			{
				if (fbxNode2 == null)
				{
					continue;
				}
				FbxMesh fbxMesh2 = new FbxMesh(fbxNode2.GetNodeAttribute(FbxNodeAttribute.EType.eMesh));
				FbxSkin fbxSkin2 = ((fbxMesh2.GetDeformerCount(FbxDeformer.EDeformerType.eSkin) != 0) ? new FbxSkin(fbxMesh2.GetDeformer(0, FbxDeformer.EDeformerType.eSkin)) : null);
				if (fbxMesh2.GetElementUV(0, FbxLayerElement.EType.eUnknown) == null)
				{
				}
				if (fbxMesh2.GetElementTangent(0) == null || fbxMesh2.GetElementBinormal(0) == null)
				{
				}
				var val2 = new FbxMatrix(fbxNode2.EvaluateGlobalTransform()).ToSharpDX();
				List<List<ushort>> list4 = new List<List<ushort>>();
				List<List<byte>> boneWeights = new List<List<byte>>();
				int num4 = 0;
				if (fbxSkin2 != null)
				{
					foreach (FbxCluster cluster3 in fbxSkin2.Clusters)
					{
						if (cluster3.ControlPointIndicesCount == 0)
						{
							continue;
						}
						int[] controlPointIndices = cluster3.GetControlPointIndices();
						double[] controlPointWeights = cluster3.GetControlPointWeights();
						FbxNode link3 = cluster3.GetLink();
						ushort num5 = ushort.MaxValue;
						if (list3.Contains(link3.Name))
						{
							num5 = (ushort)(0x8000u | ushort.Parse(link3.Name.Replace("PROC_Bone", "")));
						}
						else
						{
							num5 = (ushort)skeletonRootObject.BoneNames.IndexOf(link3.Name);
							num5 = (ushort)list.IndexOf(num5);
						}
						for (int m = 0; m < controlPointIndices.Length; m++)
						{
							int num6 = controlPointIndices[m];
							while (list4.Count <= num6)
							{
								list4.Add(new List<ushort>());
								boneWeights.Add(new List<byte>());
							}
							if ((byte)Math.Round(controlPointWeights[m] * 255.0) > 0)
							{
								list4[num6].Add(num5);
								boneWeights[num6].Add((byte)Math.Round(controlPointWeights[m] * 255.0));
							}
						}
					}
					num4 = meshSetSection.BonesPerVertex;
				}
				List<DbObject> list5 = new List<DbObject>();
				list5.AddRange(new DbObject[fbxMesh2.ControlPointsCount]);
				List<int>[] array4 = new List<int>[fbxMesh2.ControlPointsCount];
				List<int> list6 = new List<int>();
				IntPtr controlPoints = fbxMesh2.GetControlPoints();
				int num7 = 0;
				for (int n = 0; n < fbxMesh2.PolygonCount; n++)
				{
					for (int num8 = 0; num8 < 3; num8++)
					{
						int vertexIndex = fbxMesh2.GetPolygonIndex(n, num8);
						DbObject dbObject = DbObject.CreateObject();
						ushort[] newValue = null;
						byte[] newValue2 = null;
						double* ptr = (double*)(void*)(controlPoints + vertexIndex * 32);
						val3 = new Vector4((float)ptr[XAxis] * Scale, (float)ptr[YAxis] * Scale, (float)(ptr[ZAxis] * (double)FlipZ) * Scale, 1f);
						FbxLayerElementNormal elementNormal = fbxMesh2.GetElementNormal(0);
						int num9 = ((elementNormal.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (n * 3 + num8));
						int outValue = num9;
						if (elementNormal.ReferenceMode != 0)
						{
							elementNormal.IndexArray.GetAt(num9, out outValue);
						}
						elementNormal.DirectArray.GetAt(outValue, out Vector4 outValue2);
						val4 = new Vector3(outValue2.X, outValue2.Y, outValue2.Z * FlipZ);
						FbxLayerElementTangent elementTangent = fbxMesh2.GetElementTangent(0);
						int num10 = ((elementTangent.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (n * 3 + num8));
						int outValue3 = num10;
						if (elementTangent.ReferenceMode != 0)
						{
							elementTangent.IndexArray.GetAt(num10, out outValue3);
						}
						elementTangent.DirectArray.GetAt(outValue3, out Vector4 outValue4);
						//((Vector3)(ref val5))..ctor(((Vector4)(ref outValue4)).get_Item(XAxis), ((Vector4)(ref outValue4)).get_Item(YAxis), ((Vector4)(ref outValue4)).get_Item(ZAxis) * FlipZ);
						val5 = new Vector3(outValue4.X, outValue4.Y, outValue4.Z * FlipZ);

						FbxLayerElementBinormal elementBinormal = fbxMesh2.GetElementBinormal(0);
						int num11 = ((elementBinormal.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (n * 3 + num8));
						int outValue5 = num11;
						if (elementBinormal.ReferenceMode != 0)
						{
							elementBinormal.IndexArray.GetAt(num11, out outValue5);
						}
						elementBinormal.DirectArray.GetAt(outValue5, out Vector4 outValue6);
						//((Vector3)(ref val6))..ctor(((Vector4)(ref outValue6)).get_Item(XAxis), ((Vector4)(ref outValue6)).get_Item(YAxis), ((Vector4)(ref outValue6)).get_Item(ZAxis) * FlipZ);
						val6 = new Vector3(outValue6.X, outValue6.Y, outValue6.Z * FlipZ);

						if (fbxSkin2 != null)
						{
							List<ushort> list7 = new List<ushort>();
							List<byte> list8 = new List<byte>();
							var source = list4[vertexIndex].Select((ushort boneIndex, int z) => new
							{
								boneIndex = boneIndex,
								boneWeight = boneWeights[vertexIndex][z]
							});
							source = source.OrderByDescending(a => a.boneWeight);
							list7.AddRange(source.Select(a => a.boneIndex));
							list8.AddRange(source.Select(a => a.boneWeight));
							num7 = ((list7.Count > num7) ? list7.Count : num7);
							while (list7.Count > num4)
							{
								list7.RemoveRange(num4, list7.Count - num4);
								list8.RemoveRange(num4, list8.Count - num4);
							}
							int num12 = 0;
							for (int num13 = 0; num13 < list8.Count; num13++)
							{
								num12 += list8[num13];
							}
							if (num12 != 255)
							{
								for (int num14 = 0; num14 < list8.Count; num14++)
								{
									list8[num14] = (byte)Math.Round((double)(int)list8[num14] / (double)num12 * 255.0);
									if (list8[num14] <= 0)
									{
										list7[num14] = 0;
									}
								}
								num12 = 0;
								int num15 = 0;
								int index = -1;
								for (int num16 = 0; num16 < list8.Count; num16++)
								{
									num12 += list8[num16];
									if (list8[num16] > num15)
									{
										num15 = list8[num16];
										index = num16;
									}
								}
								if (num12 != 255)
								{
									list8[index] = (byte)(num15 + (255 - num12));
								}
							}
							newValue = list7.ToArray();
							newValue2 = list8.ToArray();
							Array.Sort(newValue, newValue2);
							list7.Clear();
							list8.Clear();
							int num17 = newValue.Length;
							for (int num18 = 0; num18 < 8 - num17; num18++)
							{
								list7.Add(newValue[0]);
							}
							list7.AddRange(newValue);
							list8.AddRange(new byte[8 - num17]);
							list8.AddRange(newValue2);
							newValue = list7.ToArray();
							newValue2 = list8.ToArray();
						}
						dbObject.SetValue("Pos", val3);
						dbObject.SetValue("Binormal", val6);
						dbObject.SetValue("Normal", val4);
						dbObject.SetValue("Tangent", val5);
						FbxLayerElementUV elementUV = fbxMesh2.GetElementUV(0, FbxLayerElement.EType.eUnknown);
						if (elementUV != null)
						{
							int num19 = ((elementUV.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (n * 3 + num8));
							int outValue7 = num19;
							if (elementUV.ReferenceMode != 0)
							{
								elementUV.IndexArray.GetAt(num19, out outValue7);
							}
							elementUV.DirectArray.GetAt(outValue7, out Vector2 outValue8);
							dbObject.SetValue("TexCoord" + 0, outValue8);
						}
						GeometryDeclarationDesc.Element[] elements = meshSetSection.GeometryDeclDesc[0].Elements;
						for (int num20 = 0; num20 < elements.Length; num20++)
						{
							GeometryDeclarationDesc.Element element = elements[num20];
							switch (element.Usage)
							{
								case VertexElementUsage.BoneIndices:
									dbObject.SetValue("BoneIndices", newValue);
									continue;
								case VertexElementUsage.BoneWeights:
									dbObject.SetValue("BoneWeights", newValue2);
									continue;
								case VertexElementUsage.Color0:
									{
										FbxLayerElementVertexColor elementVertexColor4 = fbxMesh2.GetElementVertexColor(0);
										if (elementVertexColor4 != null)
										{
											int num24 = ((elementVertexColor4.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (n * 3 + num8));
											int outValue15 = num24;
											if (elementVertexColor4.ReferenceMode != 0)
											{
												elementVertexColor4.IndexArray.GetAt(num24, out outValue15);
											}
											elementVertexColor4.DirectArray.GetAt(outValue15, out Vector4 outValue16);
											dbObject.SetValue("Color0", outValue16);
										}
										continue;
									}
								case VertexElementUsage.Color1:
									{
										FbxLayerElementVertexColor elementVertexColor3 = fbxMesh2.GetElementVertexColor(1);
										if (elementVertexColor3 != null)
										{
											int num23 = ((elementVertexColor3.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (n * 3 + num8));
											int outValue13 = num23;
											if (elementVertexColor3.ReferenceMode != 0)
											{
												elementVertexColor3.IndexArray.GetAt(num23, out outValue13);
											}
											elementVertexColor3.DirectArray.GetAt(outValue13, out Vector4 outValue14);
											dbObject.SetValue("Color1", outValue14);
										}
										continue;
									}
								case VertexElementUsage.MaskUv:
									{
										FbxLayerElementUV elementUV2 = fbxMesh2.GetElementUV("MaskUv");
										if (elementUV2 != null)
										{
											int num25 = ((elementUV2.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (n * 3 + num8));
											int outValue17 = num25;
											if (elementUV2.ReferenceMode != 0)
											{
												elementUV2.IndexArray.GetAt(num25, out outValue17);
											}
											elementUV2.DirectArray.GetAt(outValue17, out Vector2 outValue18);
											dbObject.SetValue("MaskUv", outValue18);
										}
										continue;
									}
								case VertexElementUsage.Delta:
									{
										FbxLayerElementVertexColor elementVertexColor2 = fbxMesh2.GetElementVertexColor("Delta");
										if (elementVertexColor2 != null)
										{
											int num22 = ((elementVertexColor2.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (n * 3 + num8));
											int outValue11 = num22;
											if (elementVertexColor2.ReferenceMode != 0)
											{
												elementVertexColor2.IndexArray.GetAt(num22, out outValue11);
											}
											elementVertexColor2.DirectArray.GetAt(outValue11, out Vector4 outValue12);
											dbObject.SetValue("Delta", outValue12);
										}
										continue;
									}
								case VertexElementUsage.BlendWeights:
									{
										FbxLayerElementVertexColor elementVertexColor5 = fbxMesh2.GetElementVertexColor("BlendWeights");
										if (elementVertexColor5 != null)
										{
											int num26 = ((elementVertexColor5.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (n * 3 + num8));
											int outValue19 = num26;
											if (elementVertexColor5.ReferenceMode != 0)
											{
												elementVertexColor5.IndexArray.GetAt(num26, out outValue19);
											}
											elementVertexColor5.DirectArray.GetAt(outValue19, out Vector4 outValue20);
											dbObject.SetValue("Delta", (float)(int)outValue20.X / 255f);
										}
										continue;
									}
								case VertexElementUsage.RegionIds:
									{
										FbxLayerElementVertexColor elementVertexColor = fbxMesh2.GetElementVertexColor("RegionIds");
										if (elementVertexColor != null)
										{
											int num21 = ((elementVertexColor.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (n * 3 + num8));
											int outValue9 = num21;
											if (elementVertexColor.ReferenceMode != 0)
											{
												elementVertexColor.IndexArray.GetAt(num21, out outValue9);
											}
											elementVertexColor.DirectArray.GetAt(outValue9, out Vector4 outValue10);
											dbObject.SetValue("Delta", (int)outValue10.X);
										}
										continue;
									}
								case VertexElementUsage.Unknown:
								case VertexElementUsage.Pos:
								case VertexElementUsage.BoneIndices2:
								case VertexElementUsage.BoneWeights2:
								case VertexElementUsage.Normal:
								case VertexElementUsage.Tangent:
								case VertexElementUsage.Binormal:
								case VertexElementUsage.BinormalSign:
								case VertexElementUsage.TexCoord0:
								case VertexElementUsage.DisplacementMapTexCoord:
								case VertexElementUsage.RadiosityTexCoord:
								case VertexElementUsage.TangentSpace:
									continue;
							}
							if ((int)element.Usage >= 34 && (int)element.Usage <= 40)
							{
								int num27 = (int)(element.Usage - 33);
								FbxLayerElementUV elementUV3 = fbxMesh2.GetElementUV(num27, FbxLayerElement.EType.eUnknown);
								if (elementUV3 != null)
								{
									int num28 = ((elementUV3.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (n * 3 + num8));
									int outValue21 = num28;
									if (elementUV3.ReferenceMode != 0)
									{
										elementUV3.IndexArray.GetAt(num28, out outValue21);
									}
									elementUV3.DirectArray.GetAt(outValue21, out Vector2 outValue22);
									dbObject.SetValue("TexCoord" + num27, outValue22);
								}
								continue;
							}
						}
						int num29 = -1;
						if (array4[vertexIndex] != null)
						{
							foreach (int item2 in array4[vertexIndex])
							{
								if (list5[item2].GetValue<Vector4>("Pos") != dbObject.GetValue<Vector4>("Pos") || list5[item2].GetValue<Vector3>("Normal") != dbObject.GetValue<Vector3>("Normal") || list5[item2].GetValue<Vector3>("Tangent") != dbObject.GetValue<Vector3>("Tangent") || list5[item2].GetValue<Vector3>("Binormal") != dbObject.GetValue<Vector3>("Binormal"))
								{
									continue;
								}
								for (int num30 = 0; num30 < 8; num30++)
								{
									string name = "TexCoord" + num30;
									if (dbObject.HasValue(name) && list5[item2].GetValue<Vector2>(name) != dbObject.GetValue<Vector2>(name))
									{
										num29 = -2;
										break;
									}
								}
								if (num29 == -1)
								{
									num29 = item2;
									break;
								}
							}
						}
						else
						{
							array4[vertexIndex] = new List<int>();
						}
						if (num29 < 0)
						{
							if (list5[vertexIndex] == null)
							{
								list5[vertexIndex] = dbObject;
								array4[vertexIndex].Add(vertexIndex);
								list6.Add(vertexIndex);
							}
							else
							{
								list5.Add(dbObject);
								array4[vertexIndex].Add(list5.Count - 1);
								list6.Add(list5.Count - 1);
							}
						}
						else
						{
							list6.Add(num29);
						}
					}
				}
				List<int> list9 = new List<int>(new int[list5.Count]);
				int num31 = 0;
				int num32 = 0;
				while (num32 < list5.Count)
				{
					if (list5[num32] == null)
					{
						list5.RemoveAt(num32);
						num32--;
					}
					else
					{
						list9[num31] = num32;
					}
					num32++;
					num31++;
				}
				using (NativeWriter nativeWriter = new NativeWriter(verticesBuffer, leaveOpen: true))
				{
					nativeWriter.BaseStream.Position = nativeWriter.BaseStream.Length;
					int num33 = 0;
					GeometryDeclarationDesc.Stream[] streams = meshSetSection.GeometryDeclDesc[0].Streams;
					for (int num20 = 0; num20 < streams.Length; num20++)
					{
						GeometryDeclarationDesc.Stream stream = streams[num20];
						if (stream.VertexStride == 0)
						{
							continue;
						}
						foreach (DbObject item3 in list5)
						{
							Vector4 value = item3.GetValue<Vector4>("Pos");
							Vector3 val7 = Vector3.Transform(new Vector3(value.X, value.Y, value.Z), val2);
							Vector3 val8 = Vector3.TransformNormal(item3.GetValue<Vector3>("Normal"), val2);
							Vector3 val9 = Vector3.TransformNormal(item3.GetValue<Vector3>("Tangent"), val2);
							Vector3 val10 = Vector3.TransformNormal(item3.GetValue<Vector3>("Binormal"), val2);
							ushort[] value2 = item3.GetValue<ushort[]>("BoneIndices");
							byte[] value3 = item3.GetValue<byte[]>("BoneWeights");
							int num34 = 0;
							GeometryDeclarationDesc.Element[] elements = meshSetSection.GeometryDeclDesc[0].Elements;
							for (int num35 = 0; num35 < elements.Length; num35++)
							{
								GeometryDeclarationDesc.Element element2 = elements[num35];
								if (element2.Usage == VertexElementUsage.Unknown)
								{
									continue;
								}
								if (num34 >= num33 && num34 < num33 + stream.VertexStride)
								{
									switch (element2.Usage)
									{
										case VertexElementUsage.Pos:
											if (element2.Format == VertexElementFormat.Float3 || element2.Format == VertexElementFormat.Float4)
											{
												nativeWriter.Write(val7.X);
												nativeWriter.Write(val7.Y);
												nativeWriter.Write(val7.Z);
												if (element2.Format == VertexElementFormat.Float4)
												{
													nativeWriter.Write(1f);
												}
											}
											else if (element2.Format == VertexElementFormat.Half3 || element2.Format == VertexElementFormat.Half4)
											{
												nativeWriter.Write(HalfUtils.Pack(val7.X));
												nativeWriter.Write(HalfUtils.Pack(val7.Y));
												nativeWriter.Write(HalfUtils.Pack(val7.Z));
												if (element2.Format == VertexElementFormat.Half4)
												{
													nativeWriter.Write(HalfUtils.Pack(1f));
												}
											}
											break;
										case VertexElementUsage.BinormalSign:
											if (element2.Format == VertexElementFormat.Half)
											{
												nativeWriter.Write(HalfUtils.Pack((Vector3.Dot(Vector3.Cross(val8, val9), val10) < 0f) ? 1f : (-1f)));
											}
											else
											{
												nativeWriter.Write((Vector3.Dot(Vector3.Cross(val8, val9), val10) < 0f) ? 1f : (-1f));
											}
											break;
										case VertexElementUsage.BoneIndices:
											if (element2.Format == VertexElementFormat.UShort4)
											{
												nativeWriter.Write(value2[4]);
												nativeWriter.Write(value2[5]);
												nativeWriter.Write(value2[6]);
												nativeWriter.Write(value2[7]);
											}
											else
											{
												nativeWriter.Write((byte)value2[4]);
												nativeWriter.Write((byte)value2[5]);
												nativeWriter.Write((byte)value2[6]);
												nativeWriter.Write((byte)value2[7]);
											}
											break;
										case VertexElementUsage.BoneWeights:
											nativeWriter.Write(value3[4]);
											nativeWriter.Write(value3[5]);
											nativeWriter.Write(value3[6]);
											nativeWriter.Write(value3[7]);
											break;
										case VertexElementUsage.BoneIndices2:
											if (element2.Format == VertexElementFormat.UShort4)
											{
												nativeWriter.Write(value2[0]);
												nativeWriter.Write(value2[1]);
												nativeWriter.Write(value2[2]);
												nativeWriter.Write(value2[3]);
											}
											else
											{
												nativeWriter.Write((byte)value2[0]);
												nativeWriter.Write((byte)value2[1]);
												nativeWriter.Write((byte)value2[2]);
												nativeWriter.Write((byte)value2[3]);
											}
											break;
										case VertexElementUsage.BoneWeights2:
											nativeWriter.Write(value3[0]);
											nativeWriter.Write(value3[1]);
											nativeWriter.Write(value3[2]);
											nativeWriter.Write(value3[3]);
											break;
										case VertexElementUsage.Binormal:
											nativeWriter.Write(HalfUtils.Pack(val10.X));
											nativeWriter.Write(HalfUtils.Pack(val10.Y));
											nativeWriter.Write(HalfUtils.Pack(val10.Z));
											nativeWriter.Write(HalfUtils.Pack(1f));
											break;
										case VertexElementUsage.Normal:
											nativeWriter.Write(HalfUtils.Pack(val8.X));
											nativeWriter.Write(HalfUtils.Pack(val8.Y));
											nativeWriter.Write(HalfUtils.Pack(val8.Z));
											nativeWriter.Write(HalfUtils.Pack(1f));
											break;
										case VertexElementUsage.Tangent:
											nativeWriter.Write(HalfUtils.Pack(val9.X));
											nativeWriter.Write(HalfUtils.Pack(val9.Y));
											nativeWriter.Write(HalfUtils.Pack(val9.Z));
											nativeWriter.Write(HalfUtils.Pack(1f));
											break;
										case VertexElementUsage.TangentSpace:
											if (element2.Format == VertexElementFormat.UInt)
											{
												Vector3.Cross(val8, val9);
												Quaternion val11 = default(Quaternion);
												val11.X = val8.Y - val10.Z;
												val11.Y = val9.Z - val8.X;
												val11.Z = val10.X - val9.Y;
												val11.W = 1f + val9.X + val10.Y + val8.Z;
												//val11.Normalize();
												int num36 = FindGreatestComponent(val11);
												if (num36 == 0)
												{
													val11 = new Quaternion(val11.W, val11.X, val11.Y, val11.Z);
												}
												if (num36 == 1)
												{
													val11 = new Quaternion(val11.X, val11.W, val11.Y, val11.Z);
												}
												if (num36 == 2)
												{
													val11 = new Quaternion(val11.X, val11.Y, val11.W, val11.Z);
												}
												Vector3 val12 = new Vector3(val11.X * (float)Math.Sign(val11.W) * (float)Math.Sqrt(0.5) + 0.5f
													, val11.Y * (float)Math.Sign(val11.W) * (float)Math.Sqrt(0.5) + 0.5f
													, val11.Z * (float)Math.Sign(val11.W) * (float)Math.Sqrt(0.5) + 0.5f
													);
												//Vector3 val12 = new Vector3(val11.X, val11.Y, val11.Z) * (float)Math.Sign(val11.W) * (float)Math.Sqrt(0.5) + 0.5f;
												val13 = new Vector4(val12.X, val12.Y, val12.Z, (float)num36 / 3f);
												uint num37 = 0u;
												num37 |= (uint)(val13.X * 1023f) << 22;
												num37 |= (uint)(val13.Y * 511f) << 13;
												num37 |= (uint)(val13.Z * 1023f) << 3;
												num37 |= (uint)(val13.W * 3f) << 1;
												num37 |= ((Vector3.Dot(Vector3.Cross(val8, val9), val10) < 0f) ? 1u : 0u);
												nativeWriter.Write(num37);
											}
											break;
										case VertexElementUsage.RadiosityTexCoord:
											nativeWriter.Write(HalfUtils.Pack(0f));
											nativeWriter.Write(HalfUtils.Pack(0f));
											break;
										case VertexElementUsage.Color0:
											if (item3.HasValue("Color0"))
											{
												Vector4 value5 = item3.GetValue<Vector4>("Color0");
												nativeWriter.Write(value5.X);
												nativeWriter.Write(value5.Y);
												nativeWriter.Write(value5.Z);
												nativeWriter.Write(value5.W);
											}
											else
											{
												nativeWriter.Write(0);
											}
											break;
										case VertexElementUsage.Color1:
											if (item3.HasValue("Color1"))
											{
												Vector4 value6 = item3.GetValue<Vector4>("Color0");
												nativeWriter.Write(value6.X);
												nativeWriter.Write(value6.Y);
												nativeWriter.Write(value6.Z);
												nativeWriter.Write(value6.W);
											}
											else
											{
												nativeWriter.Write(0);
											}
											break;
										case VertexElementUsage.MaskUv:
											if (item3.HasValue("MaskUv"))
											{
												Vector2 value10 = item3.GetValue<Vector2>("MaskUv");
												nativeWriter.Write((short)((value10.X * 2f - 1f) * 32767f));
												nativeWriter.Write((short)((value10.Y * 2f - 1f) * 32767f));
											}
											else
											{
												nativeWriter.Write((ushort)0);
												nativeWriter.Write((ushort)0);
											}
											break;
										case VertexElementUsage.Delta:
											if (item3.HasValue("Delta"))
											{
												Vector4 value9 = item3.GetValue<Vector4>("Delta");
												nativeWriter.Write(HalfUtils.Pack((float)(int)value9.X * 255f));
												nativeWriter.Write(HalfUtils.Pack((float)(int)value9.Y * 255f));
												nativeWriter.Write(HalfUtils.Pack((float)(int)value9.Z * 255f));
												nativeWriter.Write(HalfUtils.Pack((float)(int)value9.W * 255f));
											}
											else
											{
												nativeWriter.Write(HalfUtils.Pack(0f));
												nativeWriter.Write(HalfUtils.Pack(0f));
												nativeWriter.Write(HalfUtils.Pack(0f));
												nativeWriter.Write(HalfUtils.Pack(0f));
											}
											break;
										case VertexElementUsage.BlendWeights:
											if (item3.HasValue("BlendWeights"))
											{
												float value8 = item3.GetValue("BlendWeights", 0f);
												nativeWriter.Write(value8);
											}
											else
											{
												nativeWriter.Write(0f);
											}
											break;
										case VertexElementUsage.RegionIds:
											if (item3.HasValue("RegionIds"))
											{
												int value7 = item3.GetValue("RegionIds", 0);
												nativeWriter.Write(value7);
											}
											else
											{
												nativeWriter.Write(0);
											}
											break;
										case VertexElementUsage.Unknown:
											break;
										default:
											if ((int)element2.Usage >= 33 && (int)element2.Usage <= 40)
											{
												string name2 = "TexCoord" + (int)(element2.Usage - 33);
												if (item3.HasValue(name2))
												{
													Vector2 value4 = item3.GetValue<Vector2>(name2);
													nativeWriter.Write(HalfUtils.Pack(value4.X));
													nativeWriter.Write(HalfUtils.Pack(1f - value4.Y));
												}
												else
												{
													nativeWriter.Write((ushort)0);
													nativeWriter.Write((ushort)0);
												}
												break;
											}
											break;
										
									}
								}
								num34 += element2.Size;
							}
						}
						num33 += stream.VertexStride;
					}
					meshSetSection.VertexCount += (uint)list5.Count;
				}
				int num38 = 0;
				for (int num39 = 0; num39 < list6.Count; num39++)
				{
					indicesBuffer.Add((uint)(num + list9[list6[num39]]));
					num38++;
				}
				meshSetSection.PrimitiveCount += (uint)(num38 / 3);
				startIndex += (uint)num38;
				num += (uint)list5.Count;
			}
		}
	*/

		private unsafe void ProcessSection(FbxNode[] sectionNodes, MeshSetLod meshLod, int sectionIndex, MemoryStream verticesBuffer, List<uint> indicesBuffer, uint vertexOffset, ref uint startIndex)
		{
			if (sectionNodes == null)
			{
				throw new ArgumentNullException("sectionNodes");
			}
			if (meshLod == null)
			{
				throw new ArgumentNullException("meshLod");
			}
			if (verticesBuffer == null)
			{
				throw new ArgumentNullException("verticesBuffer");
			}
			if (indicesBuffer == null)
			{
				throw new ArgumentNullException("indicesBuffer");
			}
			MeshSetSection meshSetSection = meshLod.Sections[sectionIndex];
			uint num = 0u;
			meshSetSection.VertexOffset = vertexOffset;
			meshSetSection.StartIndex = startIndex;
			meshSetSection.VertexCount = 0u;
			meshSetSection.PrimitiveCount = 0u;
			List<ushort> list = new List<ushort>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			dynamic val = null;
			if (settings != null && settings.SkeletonAsset != "")
			{
				val = assetManager.GetEbx(assetManager.GetEbxEntry(settings.SkeletonAsset)).RootObject;
			}
			FbxNode[] array5 = sectionNodes;
			foreach (FbxNode fbxNode in array5)
			{
				if (fbxNode == null)
				{
					continue;
				}
				FbxMesh fbxMesh = new FbxMesh(fbxNode.GetNodeAttribute(FbxNodeAttribute.EType.eMesh));
				FbxSkin fbxSkin = ((fbxMesh.GetDeformerCount(FbxDeformer.EDeformerType.eSkin) != 0) ? new FbxSkin(fbxMesh.GetDeformer(0, FbxDeformer.EDeformerType.eSkin)) : null);
				if (fbxSkin == null)
				{
					continue;
				}
				foreach (FbxCluster cluster4 in fbxSkin.Clusters)
				{
					FbxNode link = cluster4.GetLink();
					if (link.Name.StartsWith("PROC") && !list3.Contains(link.Name))
					{
						list3.Add(link.Name);
					}
				}
				if (val == null)
				{
					throw new Exception("Missing Skeleton");
				}
				for (int i = 0; i < val.BoneNames.Count; i++)
				{
					if (!list.Contains((ushort)i))
					{
						list.Add((ushort)i);
						list2.Add(val.BoneNames[i]);
					}
				}
				foreach (string item4 in list3)
				{
					ushort num12 = ushort.Parse(item4.Replace("PROC_Bone", ""));
					for (int j = 0; j <= num12; j++)
					{
						if (!list.Contains((ushort)(val.BoneNames.Count + j)))
						{
							list.Add((ushort)(val.BoneNames.Count + j));
							list2.Add("PROC_Bone" + j);
						}
					}
				}
			}
			ushort[] array2 = list.ToArray();
			string[] array3 = list2.ToArray();
			Array.Sort(array2, array3);
			meshSetSection.SetBones(array2);
			meshLod.AddBones(array2, array3);
			list.Clear();
			list.AddRange(array2);
			Vector4 val7 = default(Vector4);
			Vector3 val8 = default(Vector3);
			Vector3 val9 = default(Vector3);
			Vector3 val10 = default(Vector3);
			Vector4 val5 = default(Vector4);
			array5 = sectionNodes;
			foreach (FbxNode fbxNode2 in array5)
			{
				if (fbxNode2 == null)
				{
					continue;
				}
				FbxMesh fbxMesh2 = new FbxMesh(fbxNode2.GetNodeAttribute(FbxNodeAttribute.EType.eMesh));
				FbxSkin fbxSkin2 = ((fbxMesh2.GetDeformerCount(FbxDeformer.EDeformerType.eSkin) != 0) ? new FbxSkin(fbxMesh2.GetDeformer(0, FbxDeformer.EDeformerType.eSkin)) : null);
				if (fbxMesh2.GetElementUV(0, FbxLayerElement.EType.eUnknown) == null)
				{
					throw new Exception("Missing UVs");
				}
				if (fbxMesh2.GetElementTangent(0) == null || fbxMesh2.GetElementBinormal(0) == null)
				{
					throw new Exception("Missing Tangents");
				}
				Matrix4x4 val6 = new FbxMatrix(fbxNode2.EvaluateGlobalTransform()).ToSharpDX();
				List<List<ushort>> list4 = new List<List<ushort>>();
				List<List<byte>> boneWeights = new List<List<byte>>();
				int num34 = 0;
				if (fbxSkin2 != null)
				{
					foreach (FbxCluster cluster3 in fbxSkin2.Clusters)
					{
						if (cluster3.ControlPointIndicesCount == 0)
						{
							continue;
						}
						int[] controlPointIndices = cluster3.GetControlPointIndices();
						double[] controlPointWeights = cluster3.GetControlPointWeights();
						FbxNode link2 = cluster3.GetLink();
						ushort num35 = ushort.MaxValue;
						if (list3.Contains(link2.Name))
						{
							num35 = (ushort)(0x8000u | ushort.Parse(link2.Name.Replace("PROC_Bone", "")));
						}
						else
						{
							num35 = (ushort)val.BoneNames.IndexOf(link2.Name);
							num35 = (ushort)list.IndexOf(num35);
						}
						for (int k = 0; k < controlPointIndices.Length; k++)
						{
							int num36 = controlPointIndices[k];
							while (list4.Count <= num36)
							{
								list4.Add(new List<ushort>());
								boneWeights.Add(new List<byte>());
							}
							if ((byte)Math.Round(controlPointWeights[k] * 255.0) > 0)
							{
								list4[num36].Add(num35);
								boneWeights[num36].Add((byte)Math.Round(controlPointWeights[k] * 255.0));
							}
						}
					}
					num34 = meshSetSection.BonesPerVertex;
				}
				List<DbObject> list5 = new List<DbObject>();
				list5.AddRange(new DbObject[fbxMesh2.ControlPointsCount]);
				List<int>[] array4 = new List<int>[fbxMesh2.ControlPointsCount];
				List<int> list6 = new List<int>();
				IntPtr controlPoints = fbxMesh2.GetControlPoints();
				int num37 = 0;
				for (int l = 0; l < fbxMesh2.PolygonCount; l++)
				{
					for (int num38 = 0; num38 < 3; num38++)
					{
						int vertexIndex = fbxMesh2.GetPolygonIndex(l, num38);
						DbObject dbObject = DbObject.CreateObject();
						ushort[] newValue = null;
						byte[] newValue2 = null;
						double* ptr = (double*)(void*)(controlPoints + vertexIndex * 32);
						val7 = new Vector4((float)ptr[XAxis] * Scale, (float)ptr[YAxis] * Scale, (float)(ptr[ZAxis] * (double)FlipZ) * Scale, 1f);
						FbxLayerElementNormal elementNormal = fbxMesh2.GetElementNormal(0);
						int num39 = ((elementNormal.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (l * 3 + num38));
						int outValue = num39;
						if (elementNormal.ReferenceMode != 0)
						{
							elementNormal.IndexArray.GetAt(num39, out outValue);
						}
						elementNormal.DirectArray.GetAt(outValue, out Vector4 outValue12);
						val8 = new Vector3(outValue12.X, outValue12.Y, outValue12.Z * FlipZ);
						FbxLayerElementTangent elementTangent = fbxMesh2.GetElementTangent(0);
						int num2 = ((elementTangent.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (l * 3 + num38));
						int outValue16 = num2;
						if (elementTangent.ReferenceMode != 0)
						{
							elementTangent.IndexArray.GetAt(num2, out outValue16);
						}
						elementTangent.DirectArray.GetAt(outValue16, out Vector4 outValue17);
						val9 = new Vector3(outValue17.X, outValue17.Y, outValue17.Z * FlipZ);
						FbxLayerElementBinormal elementBinormal = fbxMesh2.GetElementBinormal(0);
						int num3 = ((elementBinormal.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (l * 3 + num38));
						int outValue18 = num3;
						if (elementBinormal.ReferenceMode != 0)
						{
							elementBinormal.IndexArray.GetAt(num3, out outValue18);
						}
						elementBinormal.DirectArray.GetAt(outValue18, out Vector4 outValue19);
						val10 = new Vector3(outValue19.X, outValue19.Y, outValue19.Z * FlipZ);
						if (fbxSkin2 != null)
						{
							List<ushort> sortedBoneIndices = new List<ushort>();
							List<byte> sortedBoneWeights = new List<byte>();
							if (vertexIndex < list4.Count)
							{
								var source = from a in list4[vertexIndex].Select((ushort boneIndex, int z) => new
								{
									boneIndex = boneIndex,
									boneWeight = boneWeights[vertexIndex][z]
								})
											 orderby a.boneWeight descending
											 select a;
								sortedBoneIndices.AddRange(source.Select(a => a.boneIndex));
								sortedBoneWeights.AddRange(source.Select(a => a.boneWeight));
								num37 = ((sortedBoneIndices.Count > num37) ? sortedBoneIndices.Count : num37);
								while (sortedBoneIndices.Count > num34)
								{
									sortedBoneIndices.RemoveRange(num34, sortedBoneIndices.Count - num34);
									sortedBoneWeights.RemoveRange(num34, sortedBoneWeights.Count - num34);
								}
								int num4 = 0;
								for (int num5 = 0; num5 < sortedBoneWeights.Count; num5++)
								{
									num4 += sortedBoneWeights[num5];
								}
								if (num4 != 255)
								{
									for (int num6 = 0; num6 < sortedBoneWeights.Count; num6++)
									{
										sortedBoneWeights[num6] = (byte)Math.Round((double)(int)sortedBoneWeights[num6] / (double)num4 * 255.0);
										if (sortedBoneWeights[num6] <= 0)
										{
											sortedBoneIndices[num6] = 0;
										}
									}
									num4 = 0;
									int num7 = 0;
									int index = -1;
									for (int num8 = 0; num8 < sortedBoneWeights.Count; num8++)
									{
										num4 += sortedBoneWeights[num8];
										if (sortedBoneWeights[num8] > num7)
										{
											num7 = sortedBoneWeights[num8];
											index = num8;
										}
									}
									if (num4 != 255 && index != -1)
									{
										sortedBoneWeights[index] = (byte)(num7 + (255 - num4));
									}
								}
								newValue = sortedBoneIndices.ToArray();
								newValue2 = sortedBoneWeights.ToArray();
								Array.Sort(newValue, newValue2);
								sortedBoneIndices.Clear();
								sortedBoneWeights.Clear();
								int num9 = newValue.Length;
								for (int num10 = 0; num10 < 8 - num9; num10++)
								{
									sortedBoneIndices.Add(newValue[0]);
								}
								sortedBoneIndices.AddRange(newValue);
								sortedBoneWeights.AddRange(new byte[8 - num9]);
								sortedBoneWeights.AddRange(newValue2);
								newValue = sortedBoneIndices.ToArray();
								newValue2 = sortedBoneWeights.ToArray();
							}
						}
						dbObject.SetValue("Pos", val7);
						dbObject.SetValue("Binormal", val10);
						dbObject.SetValue("Normal", val8);
						dbObject.SetValue("Tangent", val9);
						FbxLayerElementUV elementUV = fbxMesh2.GetElementUV(0, FbxLayerElement.EType.eUnknown);
						if (elementUV != null)
						{
							int num11 = ((elementUV.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (l * 3 + num38));
							int outValue20 = num11;
							if (elementUV.ReferenceMode != 0)
							{
								elementUV.IndexArray.GetAt(num11, out outValue20);
							}
							elementUV.DirectArray.GetAt(outValue20, out Vector2 outValue21);
							dbObject.SetValue("TexCoord" + 0, outValue21);
						}
						GeometryDeclarationDesc.Element[] elements = meshSetSection.GeometryDeclDesc[0].Elements;
						for (int num13 = 0; num13 < elements.Length; num13++)
						{
							GeometryDeclarationDesc.Element element = elements[num13];
							switch (element.Usage)
							{
								case VertexElementUsage.BoneIndices:
									dbObject.SetValue("BoneIndices", newValue);
									continue;
								case VertexElementUsage.BoneWeights:
									dbObject.SetValue("BoneWeights", newValue2);
									continue;
								case VertexElementUsage.Color0:
									{
										FbxLayerElementVertexColor elementVertexColor4 = fbxMesh2.GetElementVertexColor(0);
										if (elementVertexColor4 != null)
										{
											int num18 = ((elementVertexColor4.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (l * 3 + num38));
											int outValue7 = num18;
											if (elementVertexColor4.ReferenceMode != 0)
											{
												elementVertexColor4.IndexArray.GetAt(num18, out outValue7);
											}
											elementVertexColor4.DirectArray.GetAt(outValue7, out Color outValue8);
											dbObject.SetValue("Color0", outValue8);
										}
										continue;
									}
								case VertexElementUsage.Color1:
									{
										FbxLayerElementVertexColor elementVertexColor3 = fbxMesh2.GetElementVertexColor(1);
										if (elementVertexColor3 != null)
										{
											int num17 = ((elementVertexColor3.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (l * 3 + num38));
											int outValue5 = num17;
											if (elementVertexColor3.ReferenceMode != 0)
											{
												elementVertexColor3.IndexArray.GetAt(num17, out outValue5);
											}
											elementVertexColor3.DirectArray.GetAt(outValue5, out Color outValue6);
											dbObject.SetValue("Color1", outValue6);
										}
										continue;
									}
								case VertexElementUsage.MaskUv:
									{
										FbxLayerElementUV elementUV2 = fbxMesh2.GetElementUV("MaskUv");
										if (elementUV2 != null)
										{
											int num19 = ((elementUV2.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (l * 3 + num38));
											int outValue9 = num19;
											if (elementUV2.ReferenceMode != 0)
											{
												elementUV2.IndexArray.GetAt(num19, out outValue9);
											}
											elementUV2.DirectArray.GetAt(outValue9, out Vector2 outValue10);
											dbObject.SetValue("MaskUv", outValue10);
										}
										continue;
									}
								case VertexElementUsage.Delta:
									{
										FbxLayerElementVertexColor elementVertexColor2 = fbxMesh2.GetElementVertexColor("Delta");
										if (elementVertexColor2 != null)
										{
											int num16 = ((elementVertexColor2.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (l * 3 + num38));
											int outValue3 = num16;
											if (elementVertexColor2.ReferenceMode != 0)
											{
												elementVertexColor2.IndexArray.GetAt(num16, out outValue3);
											}
											elementVertexColor2.DirectArray.GetAt(outValue3, out Color outValue4);
											dbObject.SetValue("Delta", outValue4);
										}
										continue;
									}
								case VertexElementUsage.BlendWeights:
									{
										FbxLayerElementVertexColor elementVertexColor5 = fbxMesh2.GetElementVertexColor("BlendWeights");
										if (elementVertexColor5 != null)
										{
											int num20 = ((elementVertexColor5.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (l * 3 + num38));
											int outValue11 = num20;
											if (elementVertexColor5.ReferenceMode != 0)
											{
												elementVertexColor5.IndexArray.GetAt(num20, out outValue11);
											}
											elementVertexColor5.DirectArray.GetAt(outValue11, out Color outValue13);
											dbObject.SetValue("Delta", (float)(int)outValue13.R / 255f);
										}
										continue;
									}
								case VertexElementUsage.RegionIds:
									{
										FbxLayerElementVertexColor elementVertexColor = fbxMesh2.GetElementVertexColor("RegionIds");
										if (elementVertexColor != null)
										{
											int num15 = ((elementVertexColor.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (l * 3 + num38));
											int outValue22 = num15;
											if (elementVertexColor.ReferenceMode != 0)
											{
												elementVertexColor.IndexArray.GetAt(num15, out outValue22);
											}
											elementVertexColor.DirectArray.GetAt(outValue22, out Color outValue2);
											dbObject.SetValue("Delta", (int)outValue2.R);
										}
										continue;
									}
								case VertexElementUsage.Unknown:
								case VertexElementUsage.Pos:
								case VertexElementUsage.BoneIndices2:
								case VertexElementUsage.BoneWeights2:
								case VertexElementUsage.Normal:
								case VertexElementUsage.Tangent:
								case VertexElementUsage.Binormal:
								case VertexElementUsage.BinormalSign:
								case VertexElementUsage.TexCoord0:
								case VertexElementUsage.DisplacementMapTexCoord:
								case VertexElementUsage.RadiosityTexCoord:
								case VertexElementUsage.TangentSpace:
									continue;
							}
							if ((int)element.Usage >= 34 && (int)element.Usage <= 40)
							{
								int num21 = (int)(element.Usage - 33);
								FbxLayerElementUV elementUV3 = fbxMesh2.GetElementUV(num21, FbxLayerElement.EType.eUnknown);
								if (elementUV3 != null)
								{
									int num22 = ((elementUV3.MappingMode == EMappingMode.eByControlPoint) ? vertexIndex : (l * 3 + num38));
									int outValue14 = num22;
									if (elementUV3.ReferenceMode != 0)
									{
										elementUV3.IndexArray.GetAt(num22, out outValue14);
									}
									elementUV3.DirectArray.GetAt(outValue14, out Vector2 outValue15);
									dbObject.SetValue("TexCoord" + num21, outValue15);
								}
								continue;
							}
							throw new Exception("Unimplement Data Type");
						}
						int num23 = -1;
						if (array4[vertexIndex] != null)
						{
							foreach (int item2 in array4[vertexIndex])
							{
								if (list5[item2].GetValue<Vector4>("Pos") != dbObject.GetValue<Vector4>("Pos") || list5[item2].GetValue<Vector3>("Normal") != dbObject.GetValue<Vector3>("Normal") || list5[item2].GetValue<Vector3>("Tangent") != dbObject.GetValue<Vector3>("Tangent") || list5[item2].GetValue<Vector3>("Binormal") != dbObject.GetValue<Vector3>("Binormal"))
								{
									continue;
								}
								for (int num24 = 0; num24 < 8; num24++)
								{
									string name = "TexCoord" + num24;
									if (dbObject.HasValue(name) && list5[item2].GetValue<Vector2>(name) != dbObject.GetValue<Vector2>(name))
									{
										num23 = -2;
										break;
									}
								}
								if (num23 == -1)
								{
									num23 = item2;
									break;
								}
							}
						}
						else
						{
							array4[vertexIndex] = new List<int>();
						}
						if (num23 < 0)
						{
							if (list5[vertexIndex] == null)
							{
								list5[vertexIndex] = dbObject;
								array4[vertexIndex].Add(vertexIndex);
								list6.Add(vertexIndex);
							}
							else
							{
								list5.Add(dbObject);
								array4[vertexIndex].Add(list5.Count - 1);
								list6.Add(list5.Count - 1);
							}
						}
						else
						{
							list6.Add(num23);
						}
					}
				}
				List<int> list7 = new List<int>(new int[list5.Count]);
				int num25 = 0;
				int num26 = 0;
				while (num26 < list5.Count)
				{
					if (list5[num26] == null)
					{
						list5.RemoveAt(num26);
						num26--;
					}
					else
					{
						list7[num25] = num26;
					}
					num26++;
					num25++;
				}
				NativeWriter nativeWriter = new NativeWriter(verticesBuffer);
				nativeWriter.Position = nativeWriter.Length;
				int num27 = 0;
				GeometryDeclarationDesc.Stream[] streams = meshSetSection.GeometryDeclDesc[0].Streams;
				for (int num14 = 0; num14 < streams.Length; num14++)
				{
					GeometryDeclarationDesc.Stream stream = streams[num14];
					if (stream.VertexStride == 0)
					{
						continue;
					}
					foreach (DbObject item3 in list5)
					{
						Vector4 value = item3.GetValue<Vector4>("Pos");
						Vector3 val11 = Vector3.Transform(new Vector3(value.X, value.Y, value.Z), val6);
						Vector3 val12 = Vector3.TransformNormal(item3.GetValue<Vector3>("Normal"), val6);
						Vector3 val13 = Vector3.TransformNormal(item3.GetValue<Vector3>("Tangent"), val6);
						Vector3 val2 = Vector3.TransformNormal(item3.GetValue<Vector3>("Binormal"), val6);
						ushort[] value3 = item3.GetValue<ushort[]>("BoneIndices");
						byte[] value4 = item3.GetValue<byte[]>("BoneWeights");
						int num28 = 0;
						GeometryDeclarationDesc.Element[] elements2 = meshSetSection.GeometryDeclDesc[0].Elements;
						for (int num29 = 0; num29 < elements2.Length; num29++)
						{
							GeometryDeclarationDesc.Element element2 = elements2[num29];
							if (element2.Usage == VertexElementUsage.Unknown)
							{
								continue;
							}
							if (num28 >= num27 && num28 < num27 + stream.VertexStride)
							{
								switch (element2.Usage)
								{
									case VertexElementUsage.Pos:
										if (element2.Format == VertexElementFormat.Float3 || element2.Format == VertexElementFormat.Float4)
										{
											nativeWriter.WriteSingleLittleEndian(val11.X);
											nativeWriter.WriteSingleLittleEndian(val11.Y);
											nativeWriter.WriteSingleLittleEndian(val11.Z);
											if (element2.Format == VertexElementFormat.Float4)
											{
												nativeWriter.WriteSingleLittleEndian(1f);
											}
										}
										else if (element2.Format == VertexElementFormat.Half3 || element2.Format == VertexElementFormat.Half4)
										{
											nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(val11.X));
											nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(val11.Y));
											nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(val11.Z));
											if (element2.Format == VertexElementFormat.Half4)
											{
												nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(1f));
											}
										}
										break;
									case VertexElementUsage.BinormalSign:
										if (element2.Format == VertexElementFormat.Half)
										{
											nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack((Vector3.Dot(Vector3.Cross(val12, val13), val2) < 0f) ? 1f : (-1f)));
										}
										else
										{
											nativeWriter.WriteSingleLittleEndian((Vector3.Dot(Vector3.Cross(val12, val13), val2) < 0f) ? 1f : (-1f));
										}
										break;
									case VertexElementUsage.BoneIndices:
										if (value3 == null)
										{
											if (list4.Count <= 0)
											{
												continue;
											}
											throw new Exception("Missing bones");
										}
										if (element2.Format == VertexElementFormat.UShort4)
										{
											nativeWriter.WriteUInt16LittleEndian(value3[4]);
											nativeWriter.WriteUInt16LittleEndian(value3[5]);
											nativeWriter.WriteUInt16LittleEndian(value3[6]);
											nativeWriter.WriteUInt16LittleEndian(value3[7]);
										}
										else
										{
											nativeWriter.Write((byte)value3[4]);
											nativeWriter.Write((byte)value3[5]);
											nativeWriter.Write((byte)value3[6]);
											nativeWriter.Write((byte)value3[7]);
										}
										break;
									case VertexElementUsage.BoneWeights:
										if (value4 == null)
										{
											if (list4.Count <= 0)
											{
												continue;
											}
											throw new Exception("Missing bones");
										}
										nativeWriter.Write(value4[4]);
										nativeWriter.Write(value4[5]);
										nativeWriter.Write(value4[6]);
										nativeWriter.Write(value4[7]);
										break;
									case VertexElementUsage.BoneIndices2:
										if (value3 == null)
										{
											if (list4.Count <= 0)
											{
												continue;
											}
											throw new Exception("Missing bones");
										}
										if (element2.Format == VertexElementFormat.UShort4)
										{
											nativeWriter.WriteUInt16LittleEndian(value3[0]);
											nativeWriter.WriteUInt16LittleEndian(value3[1]);
											nativeWriter.WriteUInt16LittleEndian(value3[2]);
											nativeWriter.WriteUInt16LittleEndian(value3[3]);
										}
										else
										{
											nativeWriter.Write((byte)value3[0]);
											nativeWriter.Write((byte)value3[1]);
											nativeWriter.Write((byte)value3[2]);
											nativeWriter.Write((byte)value3[3]);
										}
										break;
									case VertexElementUsage.BoneWeights2:
										if (value4 == null)
										{
											if (list4.Count <= 0)
											{
												continue;
											}
											throw new Exception("Missing bones");
										}
										nativeWriter.Write(value4[0]);
										nativeWriter.Write(value4[1]);
										nativeWriter.Write(value4[2]);
										nativeWriter.Write(value4[3]);
										break;
									case VertexElementUsage.Binormal:
										nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(val2.X));
										nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(val2.Y));
										nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(val2.Z));
										nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(1f));
										break;
									case VertexElementUsage.Normal:
										nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(val12.X));
										nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(val12.Y));
										nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(val12.Z));
										nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(1f));
										break;
									case VertexElementUsage.Tangent:
										nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(val13.X));
										nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(val13.Y));
										nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(val13.Z));
										nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(1f));
										break;
									case VertexElementUsage.TangentSpace:
										if (element2.Format == VertexElementFormat.UInt)
										{
											Quaternion val3 = default(Quaternion);
											val3.X = val12.Y - val2.Z;
											val3.Y = val13.Z - val12.X;
											val3.Z = val2.X - val13.Y;
											val3.W = 1f + val13.X + val2.Y + val12.Z;
											val3 = Quaternion.Normalize(val3);
											int num30 = FindGreatestComponent(val3);
											if (num30 == 0)
											{
												val3 = new Quaternion(val3.W, val3.X, val3.Y, val3.Z);
											}
											if (num30 == 1)
											{
												val3 = new Quaternion(val3.X, val3.W, val3.Y, val3.Z);
											}
											if (num30 == 2)
											{
												val3 = new Quaternion(val3.X, val3.Y, val3.W, val3.Z);
											}
											Vector3 val4 = new Vector3(val3.X, val3.Y, val3.Z) * Math.Sign(val3.W) * (float)Math.Sqrt(0.5) + new Vector3(0.5f);
											val5 = new Vector4(val4.X, val4.Y, val4.Z, (float)num30 / 3f);
											uint num31 = 0u;
											num31 |= (uint)(val5.X * 1023f) << 22;
											num31 |= (uint)(val5.Y * 511f) << 13;
											num31 |= (uint)(val5.Z * 1023f) << 3;
											num31 |= (uint)(val5.W * 3f) << 1;
											num31 |= ((Vector3.Dot(Vector3.Cross(val12, val13), val2) < 0f) ? 1u : 0u);
											nativeWriter.WriteUInt32LittleEndian(num31);
										}
										break;
									case VertexElementUsage.RadiosityTexCoord:
										nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(0f));
										nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(0f));
										break;
									case VertexElementUsage.Color0:
										if (item3.HasValue("Color0"))
										{
											Color value6 = item3.GetValue<Color>("Color0");
											nativeWriter.Write(value6.R);
											nativeWriter.Write(value6.G);
											nativeWriter.Write(value6.B);
											nativeWriter.Write(value6.A);
										}
										else
										{
											nativeWriter.WriteInt32LittleEndian(0);
										}
										break;
									case VertexElementUsage.Color1:
										if (item3.HasValue("Color1"))
										{
											Color value7 = item3.GetValue<Color>("Color0");
											nativeWriter.Write(value7.R);
											nativeWriter.Write(value7.G);
											nativeWriter.Write(value7.B);
											nativeWriter.Write(value7.A);
										}
										else
										{
											nativeWriter.WriteInt32LittleEndian(0);
										}
										break;
									case VertexElementUsage.MaskUv:
										if (item3.HasValue("MaskUv"))
										{
											Vector2 value2 = item3.GetValue<Vector2>("MaskUv");
											nativeWriter.WriteInt16LittleEndian((short)((value2.X * 2f - 1f) * 32767f));
											nativeWriter.WriteInt16LittleEndian((short)((value2.Y * 2f - 1f) * 32767f));
										}
										else
										{
											nativeWriter.WriteUInt16LittleEndian(0);
											nativeWriter.WriteUInt16LittleEndian(0);
										}
										break;
									case VertexElementUsage.Delta:
										if (item3.HasValue("Delta"))
										{
											Color value10 = item3.GetValue<Color>("Delta");
											nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack((float)(int)value10.R * 255f));
											nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack((float)(int)value10.G * 255f));
											nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack((float)(int)value10.B * 255f));
											nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack((float)(int)value10.A * 255f));
										}
										else
										{
											nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(0f));
											nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(0f));
											nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(0f));
											nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(0f));
										}
										break;
									case VertexElementUsage.BlendWeights:
										if (item3.HasValue("BlendWeights"))
										{
											float value9 = item3.GetValue("BlendWeights", 0f);
											nativeWriter.WriteSingleLittleEndian(value9);
										}
										else
										{
											nativeWriter.WriteSingleLittleEndian(0f);
										}
										break;
									case VertexElementUsage.RegionIds:
										if (item3.HasValue("RegionIds"))
										{
											int value8 = item3.GetValue("RegionIds", 0);
											nativeWriter.WriteInt32LittleEndian(value8);
										}
										else
										{
											nativeWriter.WriteInt32LittleEndian(0);
										}
										break;
									default:
										if ((int)element2.Usage >= 33 && (int)element2.Usage <= 40)
										{
											string name2 = "TexCoord" + (int)(element2.Usage - 33);
											if (item3.HasValue(name2))
											{
												Vector2 value5 = item3.GetValue<Vector2>(name2);
												nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(value5.X));
												nativeWriter.WriteUInt16LittleEndian(HalfUtils.Pack(1f - value5.Y));
											}
											else
											{
												nativeWriter.WriteUInt16LittleEndian(0);
												nativeWriter.WriteUInt16LittleEndian(0);
											}
											break;
										}
										throw new Exception("Unimplement Data Type");

									case VertexElementUsage.Unknown:
										break;
								}
							}
							num28 += element2.Size;
						}
					}
					num27 += stream.VertexStride;
				}
				meshSetSection.VertexCount += (uint)list5.Count;
				int num32 = 0;
				for (int num33 = 0; num33 < list6.Count; num33++)
				{
					indicesBuffer.Add((uint)(num + list7[list6[num33]]));
					num32++;
				}
				meshSetSection.PrimitiveCount += (uint)(num32 / 3);
				startIndex += (uint)num32;
				num += (uint)list5.Count;
			}
		}
	}
}
