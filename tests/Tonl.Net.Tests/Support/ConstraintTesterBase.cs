using NUnit.Framework.Constraints;
using NUnit.Framework.Internal;

namespace Tonl.Net.Tests.Support;

public abstract class ConstraintTesterBase
{
	protected static string ExtractMessage<T>(Constraint subject, T actual)
	{
		string message = extractMessage(subject.ApplyTo(actual));
		return message;
	}

	protected static string ExtractMessage<T>(Constraint subject, ActualValueDelegate<T> actual)
	{
		string message = extractMessage(subject.ApplyTo(actual));
		return message;
	}

	private static string extractMessage(ConstraintResult result)
	{
		string message = string.Empty;

		if (!result.IsSuccess)
		{
			using var writer = new TextMessageWriter();
			result.WriteMessageTo(writer);

			message = writer.ToString();
		}

		return message;
	}

	protected static bool Matches<T>(Constraint subject, T actual)
	{
		ConstraintResult result = subject.ApplyTo(actual);
		return result.IsSuccess;
	}

	protected static bool Matches<T>(Constraint subject, ActualValueDelegate<T> actual)
	{
		ConstraintResult result = subject.ApplyTo(actual);
		return result.IsSuccess;
	}
}