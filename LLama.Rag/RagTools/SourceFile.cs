using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLama.Rag.RagTools
{
    // Data Structure for information sources
    public class SourceFile
    {
        public int Id { get; set; } // Primary Key
        public string SourceType { get; set; } = string.Empty; //file extension or type
        public string Url { get; set; } = string.Empty; // URL if applicable
        public string FileName { get; set; } = string.Empty; // Name of the file/source
        public string FileType { get; set; } = string.Empty; // Extension or file type
        internal DateTime DateLastSaved { get; set; } = DateTime.Today; // Date of creation
        public string Summary { get; set; } = string.Empty; // Summary of the source, likely needs to be obtained using LLM agent
        public string SummaryEmbeddings { get; set; } = string.Empty; // Embedding of the summary for vector matching
        public string[] KeyWords { get; set; } = Array.Empty<string>(); // Keywords for searching, likely needs to be obtained with LLM agent
        public float[] KeyWordEmbeddings { get; set; } = Array.Empty<float>();// Keyword embeddings for vector matching


        //Navigation property 
        public List<Chunk> Chunks { get; set; } = new();

        [NotMapped] //properties not added to the database
        public string TextContent { get; set; } = string.Empty; //When the sourceFile is created the 
        public List<string>? TextContentList { get; set; }
    }
}
