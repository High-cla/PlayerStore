using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using b7tdMvJlthvEYSFpZX;
using qsMcngoipwvHQ9hnKU;

namespace NhNZ2wxChepvkBEjikk;

internal static class gmci8SxeCL5l2d9lHsH
{
	internal static gmci8SxeCL5l2d9lHsH FSyD66AkK7E8mJDWjoM;

	internal static lstWCuWjydojAmdSwM AurxKpOVn5()
	{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		string text = executingAssembly.GetManifestResourceNames().Single((string name) => name.EndsWith(".Resources.item-catalog.json", StringComparison.Ordinal));
		using Stream utf8Json = executingAssembly.GetManifestResourceStream(text) ?? throw new InvalidOperationException("Embedded catalog not found: " + text);
		lstWCuWjydojAmdSwM lstWCuWjydojAmdSwM2 = JsonSerializer.Deserialize<lstWCuWjydojAmdSwM>(utf8Json, new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		}) ?? throw new InvalidDataException("Embedded item catalog is empty.");
		if (lstWCuWjydojAmdSwM2.fs4KyDFa7 != 395 || lstWCuWjydojAmdSwM2.pXailL0jO != 2 || lstWCuWjydojAmdSwM2.hsBU0We3h != 397 || lstWCuWjydojAmdSwM2.Ufvry4rPg.Count != 397 || lstWCuWjydojAmdSwM2.Ufvry4rPg.Select((lxaYMLBUJSOI9qKHmn record) => record.VXSVMCqdI).Distinct<string>(StringComparer.Ordinal).Count() != 397)
		{
			throw new InvalidDataException($"Embedded catalog contract mismatch: items={lstWCuWjydojAmdSwM2.fs4KyDFa7}, numeric={lstWCuWjydojAmdSwM2.pXailL0jO}, visible={lstWCuWjydojAmdSwM2.hsBU0We3h}, records={lstWCuWjydojAmdSwM2.Ufvry4rPg.Count}.");
		}
		return lstWCuWjydojAmdSwM2;
	}

	internal static bool A9r1FdAYFBLtOJl8ahJ()
	{
		return FSyD66AkK7E8mJDWjoM == null;
	}

	internal static gmci8SxeCL5l2d9lHsH YUHcrWAZ9783NpWr911()
	{
		return FSyD66AkK7E8mJDWjoM;
	}
}
