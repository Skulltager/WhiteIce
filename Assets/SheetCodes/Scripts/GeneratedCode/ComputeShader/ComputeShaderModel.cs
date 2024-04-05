using System;
using UnityEngine;
namespace SheetCodes
{
	//Generated code, this script is only generated once and will not be overwritted!
	//You can add code to this script to iterate your records but don't remove the generated code!

	[Serializable]
	public class ComputeShaderModel : BaseModel<ComputeShaderRecord, ComputeShaderIdentifier>
	{
		[SerializeField] private ComputeShaderRecord[] records = default;
		protected override ComputeShaderRecord[] Records { get { return records; } }

		//Add your code below this line
	}
}
