using System;
using System.Collections;
using System.Reflection;
using Prepatcher;

namespace SKFacilityCrashFix
{
	// Core SK (skyarkhangel.HSK) still tries to patch the old
	// "VanillaStorytellersExpanded.StorytellerWatcher:ExposeData" method.
	// Vanilla Expanded Framework 1.6 moved that type to
	// "VEF.Storyteller.StorytellerWatcher:ExposeData", so AccessTools.Method
	// returns null and Harmony throws "Null method for skyarkhangel.HSK",
	// aborting the SK.Controller constructor.
	//
	// RimWorld 1.6 also has a bug in Verse.Def.ConfigErrors: when a def has an
	// empty description it reports "empty description" and then indexes
	// description[0], throwing IndexOutOfRangeException for every such def.
	//
	// Both issues are fixed here with Prepatcher rewrites that run before the
	// affected assemblies are used. Reflection is used because the Mono.Cecil
	// types bundled with Prepatcher are internal to its Harmony assembly.
	public static class HSKVEFNamespaceFix
	{
		private const string OldMethod = "VanillaStorytellersExpanded.StorytellerWatcher:ExposeData";
		private const string NewMethod = "VEF.Storyteller.StorytellerWatcher:ExposeData";

		[FreePatchAll]
		private static bool Rewrite(object module)
		{
			// The ConfigErrors rewrite is disabled: on RimWorld 1.6.4871 it
			// produced invalid IL (InvalidProgramException in MoveNext).
			// Empty-description defs are handled with ignoreConfigErrors XML
			// patches instead.
			return RewriteCoreSK(module);
		}

		private static bool RewriteCoreSK(object module)
		{
			Type moduleType = module.GetType();
			MethodInfo getTypeMethod = moduleType.GetMethod("GetType", new[] { typeof(string) });
			object type = getTypeMethod.Invoke(module, new object[] { "SK.ModPatches" });
			if (type == null)
			{
				return false;
			}

			object method = FindMethod(type, "Init");
			if (method == null)
			{
				return false;
			}

			object body = GetProperty(method, "Body");
			if (body == null)
			{
				return false;
			}

			IEnumerable instructions = (IEnumerable)GetProperty(body, "Instructions");
			bool changed = false;
			foreach (object instruction in instructions)
			{
				PropertyInfo operandProperty = instruction.GetType().GetProperty("Operand");
				string value = operandProperty.GetValue(instruction, null) as string;
				if (value == OldMethod)
				{
					operandProperty.SetValue(instruction, NewMethod, null);
					changed = true;
				}
			}

			return changed;
		}

		private static bool RewriteConfigErrors(object module)
		{
			object type = FindType(module, "Verse.Def/<ConfigErrors>d__23");
			if (type == null)
			{
				return false;
			}

			object method = FindMethod(type, "MoveNext");
			if (method == null)
			{
				return false;
			}

			object body = GetProperty(method, "Body");
			if (body == null)
			{
				return false;
			}

			object instructionsObj = GetProperty(body, "Instructions");
			int count = (int)GetProperty(instructionsObj, "Count");
			object guard = null;
			int guardIndex = -1;
			object lengthMethod = null;

			for (int i = 0; i < count; i++)
			{
				object instruction = GetItem(instructionsObj, i);
				string opName = GetOpName(instruction);

				if (opName == "callvirt" && OperandText(instruction).Contains("String::get_Length"))
				{
					lengthMethod = GetProperty(instruction, "Operand");
				}

				if ((opName == "brfalse" || opName == "brfalse.s") && i > 0)
				{
					object previous = GetItem(instructionsObj, i - 1);
					if (IsDescriptionFieldLoad(previous))
					{
						guard = instruction;
						guardIndex = i;
					}
				}
			}

			if (guard == null || lengthMethod == null)
			{
				return false;
			}

			object target = GetProperty(guard, "Operand");
			object sample = guard;
			object dup = CreateInstruction(sample, "Dup", null);
			object branchNonEmpty = CreateInstruction(sample, "Brtrue", target);
			object pop = CreateInstruction(sample, "Pop", null);
			object branchSkip = CreateInstruction(sample, "Br", target);
			object callLength = CreateInstruction(sample, "Callvirt", lengthMethod);
			object loadZero = CreateInstruction(sample, "Ldc_I4_0", null);
			object branchEmpty = CreateInstruction(sample, "Beq", target);

			MethodInfo insert = instructionsObj.GetType().GetMethod("Insert");
			object[] newInstructions = new object[] { dup, branchNonEmpty, pop, branchSkip, callLength, loadZero, branchEmpty };
			for (int j = 0; j < newInstructions.Length; j++)
			{
				insert.Invoke(instructionsObj, new object[] { guardIndex + j, newInstructions[j] });
			}

			SetOpCode(guard, CreateInstruction(sample, "Nop", null));
			SetProperty(guard, "Operand", null);
			return true;
		}

		private static bool IsDescriptionFieldLoad(object instruction)
		{
			return GetOpName(instruction) == "ldfld" &&
				OperandText(instruction).Trim().EndsWith("Verse.Def::description");
		}

		private static string OperandText(object instruction)
		{
			object operand = GetProperty(instruction, "Operand");
			return operand == null ? "" : operand.ToString();
		}

		private static object CreateInstruction(object sampleInstruction, string opCodeName, object operand)
		{
			Type instructionType = sampleInstruction.GetType();
			Type opCodeType = GetProperty(sampleInstruction, "OpCode").GetType();
			Type opCodesType = instructionType.Assembly.GetType("Mono.Cecil.Cil.OpCodes");
			object opCode = opCodesType.GetField(opCodeName).GetValue(null);

			if (operand == null)
			{
				MethodInfo create = instructionType.GetMethod("Create", new[] { opCodeType });
				return create.Invoke(null, new object[] { opCode });
			}

			foreach (MethodInfo create in instructionType.GetMethods())
			{
				ParameterInfo[] parameters = create.GetParameters();
				if (create.Name == "Create" && parameters.Length == 2 &&
					parameters[0].ParameterType == opCodeType &&
					parameters[1].ParameterType.IsAssignableFrom(operand.GetType()))
				{
					return create.Invoke(null, new object[] { opCode, operand });
				}
			}

			throw new InvalidOperationException("No matching Instruction.Create overload for " + opCodeName);
		}

		private static void SetOpCode(object instruction, object otherInstruction)
		{
			object opCode = GetProperty(otherInstruction, "OpCode");
			SetProperty(instruction, "OpCode", opCode);
		}

		private static object FindType(object module, string fullName)
		{
			MethodInfo getTypeMethod = module.GetType().GetMethod("GetType", new[] { typeof(string) });
			return getTypeMethod.Invoke(module, new object[] { fullName });
		}

		private static string GetOpName(object instruction)
		{
			object opCode = GetProperty(instruction, "OpCode");
			return (string)GetProperty(opCode, "Name");
		}

		private static object GetProperty(object target, string name)
		{
			return target.GetType().GetProperty(name).GetValue(target, null);
		}

		private static void SetProperty(object target, string name, object value)
		{
			target.GetType().GetProperty(name).SetValue(target, value, null);
		}

		private static object GetItem(object collection, int index)
		{
			return collection.GetType().GetProperty("Item").GetValue(collection, new object[] { index });
		}

		private static object FindMethod(object type, string name)
		{
			IEnumerable methods = (IEnumerable)GetProperty(type, "Methods");
			foreach (object method in methods)
			{
				if ((string)GetProperty(method, "Name") == name)
				{
					return method;
				}
			}

			return null;
		}
	}
}
