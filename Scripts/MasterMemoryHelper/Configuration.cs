using System;
using TypeReferences;
using UnityEngine;
using MasterMemory;

namespace MasterMemoryHelper
{
	[CreateAssetMenu(fileName = "Configuration", menuName = "MasterMemoryHelper/Configuration")]
	public class Configuration : ScriptableObject
	{
		[SerializeField]
		private string m_scriptInputPath = string.Empty;
		[SerializeField]
		private string m_scriptOutputPath = string.Empty;
		[SerializeField]
		private string m_csvInputPath = string.Empty;
		[SerializeField]
		private string m_binaryOutputPath = string.Empty;
		[SerializeField]
		private string m_namespace = string.Empty;
		[SerializeField]
		private string m_prefixClassName = string.Empty;
		[SerializeField, ClassExtends(typeof(DatabaseBuilderBase))]
		private ClassTypeReference m_databaseBuilderType = default;
		[SerializeField, ClassExtends(typeof(MemoryDatabaseBase))]
		private ClassTypeReference m_memoryDatabaseType = default;

		public string ScriptInputPath => m_scriptInputPath;
		public string ScriptOutputPath => m_scriptOutputPath;
		public string CsvInputPath => m_csvInputPath;
		public string BinaryOutputPath => m_binaryOutputPath;
		public string Namespace => m_namespace;
		public string PrefixClassName => m_prefixClassName;

		public Type DatabaseBuilderType => m_databaseBuilderType.Type;
		public Type MemoryDatabaseType => m_memoryDatabaseType.Type;
	}
}
