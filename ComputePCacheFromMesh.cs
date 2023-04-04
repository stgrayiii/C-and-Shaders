PCache ComputePCacheFromMesh()
    {
            var meshCache = ComputeDataCache(m_Mesh);

            Picker picker = null;
            if (m_Distribution == Distribution.Sequential)
            {
                if (m_MeshBakeMode == MeshBakeMode.Vertex)
                {
                    picker = new SequentialPickerVertex(meshCache);
                }
                else if (m_MeshBakeMode == MeshBakeMode.Triangle)
                {
                    picker = new SequentialPickerTriangle(meshCache);
                }
            }
            else if (m_Distribution == Distribution.Random)
            {
                if (m_MeshBakeMode == MeshBakeMode.Vertex)
                {
                    picker = new RandomPickerVertex(meshCache, m_SeedMesh);
                }
                else if (m_MeshBakeMode == MeshBakeMode.Triangle)
                {
                    picker = new RandomPickerTriangle(meshCache, m_SeedMesh);
                }
            }
            else if (m_Distribution == Distribution.RandomUniformArea)
            {
                picker = new RandomPickerUniformArea(meshCache, m_SeedMesh);
            }
            if (picker == null)
                throw new InvalidOperationException("Unable to find picker");

            var positions = new List<Vector3>();
            var normals = m_ExportNormals ? new List<Vector3>() : null;
            var colors = m_ExportColors ? new List<Vector4>() : null;
            var uvs = m_ExportUV ? new List<Vector4>() : null;

            if (m_isMask)
            {
                int pointCount = 0;
                int maxIteration = m_OutputPointCount * 1000;
                int iteration = 0;
                    while (pointCount < m_OutputPointCount)
                    {
                        if (++iteration > maxIteration)
                            throw new InvalidOperationException("Can't generate this point cache in a reasonable amount of time.");

                        if (iteration % 64 == 0)
                        {
                            var cancel = EditorUtility.DisplayCancelableProgressBar("pCache bake tool", string.Format("Sampling data... {0}/{1}", pointCount, m_OutputPointCount), (float) pointCount / (float) m_OutputPointCount);
                            if (cancel)
                            {
                                return null;
                            }
                        }
                        
                        var vertex = picker.GetNext();
                        
                        if (m_Mask != null && vertex.uvs.Any())
                        {
                            //Obtain the color of the input mask at the UV location passed in from the current vertex
                            var colorSample = m_Mask.GetPixelBilinear(vertex.uvs[0].x, vertex.uvs[0].y)
                            ;
                            //Add the position of this vertex to the pCache only if the color of the mask at this location has a value written to it
                            if (colorSample == Color.black || colorSample.a <= 0)
                            {
                                continue;
                            }

                            positions.Add(vertex.position);
                            if (m_ExportNormals) normals.Add(vertex.normal);
                            if (m_ExportColors) colors.Add(vertex.color);
                            if (m_ExportUV) uvs.Add(vertex.uvs.Any() ? vertex.uvs[0] : Vector4.zero);
                        }
                    }
            }
            else
            {
                for (int i = 0; i < m_OutputPointCount; ++i)
                {
                    if (i % 64 == 0)
                    {
                        var cancel = EditorUtility.DisplayCancelableProgressBar("pCache bake tool", string.Format("Sampling data... {0}/{1}", i, m_OutputPointCount), (float) i / (float) m_OutputPointCount);
                        if (cancel)
                        {
                            return null;
                        }
                    }

                    var vertex = picker.GetNext();
                    positions.Add(vertex.position);
                    if (m_ExportNormals) normals.Add(vertex.normal);
                    if (m_ExportColors) colors.Add(vertex.color);
                    if (m_ExportUV) uvs.Add(vertex.uvs.Any() ? vertex.uvs[0] : Vector4.zero);
                }
            }

            var file = new PCache();
            file.AddVector3Property("position");
            if (m_ExportNormals) file.AddVector3Property("normal");
            if (m_ExportColors) file.AddColorProperty("color");
            if (m_ExportUV) file.AddVector4Property("uv");

            EditorUtility.DisplayProgressBar("pCache bake tool", "Generating pCache...", 0.0f);
            file.SetVector3Data("position", positions);
            if (m_ExportNormals) file.SetVector3Data("normal", normals);
            if (m_ExportColors) file.SetColorData("color", colors);
            if (m_ExportUV) file.SetVector4Data("uv", uvs);

            EditorUtility.ClearProgressBar();
            return file;
        }