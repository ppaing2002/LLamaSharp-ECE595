using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLama.Rag.RagTools
{
    //Chunk data structure
    public class Chunk()
    {
        public int Id { get; set; }                               // Primary Key for Chunks
        public int SourceId { get; set; }                         // Original SourceID Foriegn Key
        public string SourceURL { get; set; } = string.Empty;     // Optional: URL or file path
        public int ChunkIndex { get; set; }                       // Order of the chunk within the source file
        public string Text { get; set; } = string.Empty;          // Raw chunk text
        public float[] Embedding { get; set; } = Array.Empty<float>();     //Embedding Vector. Would be good to compress this somehow..'?

        //public Dictionary<string, string> Metadata { get; set; } // Any additional info... not sure this can be stored? 

        //Navigation property
        public SourceFile Source { get; set; } = null!;

    }

}
