using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLama.Rag
{
    class SimilaryMeasures
    {
        public static double CosineSimilarity(float[] vector1, float[] vector2)
        {
            double dotProduct = 0.0, magnitude1 = 0.0, magnitude2 = 0.0;
            int length = Math.Min(vector1.Length, vector2.Length);

            for (int i = 0; i < length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += Math.Pow(vector1[i], 2);
                magnitude2 += Math.Pow(vector2[i], 2);
            }

            return dotProduct / (Math.Sqrt(magnitude1) * Math.Sqrt(magnitude2));
        }


        public static double ComputeSimilarity(IReadOnlyList<float[]> queryVectors, List<float[]> embeddedVectors)
        {
            int queryVectorDimension = queryVectors[0].Length;
            int embeddedVectorsDimension = embeddedVectors[0].Length;

            float[] queryVectorMean = Enumerable.Range(0, queryVectorDimension)
                    .AsParallel()  // Enable parallel processing
                    .Select(i => queryVectors.AsParallel().Average(vec => vec[i]))  // Compute mean per dimension in parallel
                    .ToArray();

            float[] embeddedVectorsMean = Enumerable.Range(0, embeddedVectorsDimension)
                   .AsParallel()  // Enable parallel processing
                   .Select(i => embeddedVectors.AsParallel().Average(vec => vec[i]))  // Compute mean per dimension in parallel
                   .ToArray();


            return CosineSimilarity(queryVectorMean, embeddedVectorsMean);
        }
        public static double ComputeSimilarity(IReadOnlyList<float[]> queryVectors, float[] embeddedVector)
        {
            int queryVectorDimension = queryVectors[0].Length;
            int embeddedVectorsDimension = 1;

            float[] queryVectorMean = Enumerable.Range(0, queryVectorDimension)
                    .AsParallel()  // Enable parallel processing
                    .Select(i => queryVectors.AsParallel().Average(vec => vec[i]))  // Compute mean per dimension in parallel
                    .ToArray();

            float[] embeddedVectorsMean = Enumerable.Range(0, embeddedVectorsDimension)
                   .AsParallel()  // Enable parallel processing
                   .Select(i => embeddedVector.AsParallel().Average())  // Compute mean per dimension in parallel
                   .ToArray();


            return CosineSimilarity(queryVectorMean, embeddedVectorsMean);
        }

    }
}
