using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace LLama.Rag.RagTools
{
    interface IStorageHandler
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns a DataTable of all embeddings</returns>
        Task<DataTable> GetAllChunks();

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns a DataTable of all embedding sources</returns>
        Task<DataTable> GetAllSources();

        //Need to implement
        Task<DataTable> GetChunksBySourceID(int Id);

        Task SaveSource(SourceFile Source, bool Overwrite);

    }
}
