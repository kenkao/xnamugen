﻿using System;
using System.Diagnostics;
using xnaMugen.Collections;

namespace xnaMugen.Evaluation.Operators
{
	class GreaterEquals : IFunction
	{
		public EvaluationResult Evaluate(EvaluationState state, ReadOnlyList<IEvaluate> args, ReadOnlyList<Object> data)
		{
			return BaseOperations.GreaterEquals(args[0].Evaluate(state), args[1].Evaluate(state));
		}
	}
}