using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.CodeDom;
using System.Linq;


/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public abstract class Script_Instance_8234d : GH_ScriptInstance
{
  #region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { /* Implementation hidden. */ }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { /* Implementation hidden. */ }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { /* Implementation hidden. */ }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { /* Implementation hidden. */ }
  #endregion

  #region Members
  /// <summary>Gets the current Rhino document.</summary>
  private readonly RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private readonly GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private readonly IGH_Component Component;
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private readonly int Iteration;
  #endregion
  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments,
  /// Output parameters as ref arguments. You don't have to assign output parameters,
  /// they will have a default value.
  /// </summary>
  #region Runscript
  private void RunScript(List<string> datas, int SalvageTimberTotal, ref object score, ref object totalOffcuts, ref object totalSalvageLength, ref object materialEfficiency, ref object offcutsCount, ref object laborEfficiency, ref object reuseCount, ref object minCutRatio)
  {
    List<double> timberALs = new List<double>();
    List<double> timberBLs = new List<double>();
    List<double> timberBLs_Clean = new List<double>();
    List<double> offcuts = new List<double>();
    List<double> cuts = new List<double>();
    int _reuseCount = 0;
    int _minCutCount = 0;
    DeSerialize(datas, out timberALs, out timberBLs, out offcuts, out cuts, out _reuseCount, out _minCutCount);

    // count all salvage timber that were used
    int salvageTimberUsedCount = timberALs.Count;
    foreach (double timberB in timberBLs)
    {
      if (!double.IsNaN(timberB))
      {
        salvageTimberUsedCount ++;
        timberBLs_Clean.Add(timberB);
      }
    }
    // total offcuts calculation
    double _totalOffcuts = offcuts.Sum();

    // total salvage timber length
    double _totalSalvageLength = timberALs.Sum() + timberBLs_Clean.Sum();

    // length usage efficiency
    double _materialEfficiency = _totalOffcuts/ _totalSalvageLength ;
    _materialEfficiency = (1.0 / _materialEfficiency) * 10;

    // labor efficiency
    int _offcutsCount = CountOffcuts(offcuts);
    double _laborEfficiency = (double)datas.Count/ (double)_offcutsCount * 10;

    // material reuse efficiency
    double _materialReuseEfficiency = (double)_reuseCount * 2;

    // min cut ratio
    double _minCutRatio =  (double)_minCutCount / (double)datas.Count * 100;

    // calculation
    double calculation = (_laborEfficiency / 2.0) + (_materialEfficiency / 2.0) + _materialReuseEfficiency + _minCutRatio;


    score = Math.Round(calculation, 5);
    totalOffcuts = _totalOffcuts;
    totalSalvageLength = _totalSalvageLength;
    materialEfficiency = _materialEfficiency;
    offcutsCount = _offcutsCount;
    laborEfficiency = _laborEfficiency;
    reuseCount = _reuseCount;
    minCutRatio = _minCutRatio;
  }
  #endregion
  #region Additional

  private void DeSerialize(List<string> datas, out List<double> timberALs, out List<double> timberBLs, out List<double> offcuts, out List<double> cuts, out int reuseCount, out int minCutCount)
  {
    timberALs = new List<double>();
    timberBLs = new List<double>();
    offcuts = new List<double>();
    cuts = new List<double>();
    minCutCount = 0;
    reuseCount = 0;

    Dictionary<string, int> reuse_count_dict = new Dictionary<string, int>();

    foreach (var data in datas)
    {
      // Check if the string contains square brackets
      if (data.Contains("[") && data.Contains("]"))
      {
        double cut = double.Parse(ParseString(data, "[", "]"));
        cuts.Add(cut);
        string[] timber = data.Split(new string[] { "<", ">" }, StringSplitOptions.RemoveEmptyEntries);
        string timber_name = timber[1].Split(',')[0];

        if (reuse_count_dict.ContainsKey(timber_name))
        {
          reuse_count_dict[timber_name]++;
          if (reuse_count_dict[timber_name] >= 2)
          {
            reuseCount++;
          }
        }
        else
        {
          reuse_count_dict[timber_name] = 1;
        }
      }

      // min cut count
      string cutMessage = ParseString(data, "{", "}");
      if (cutMessage == "minCut")
      {
        minCutCount++;
      }


      string[] offcutsData = data.Split(new string[] { "|", "|" }, StringSplitOptions.RemoveEmptyEntries);
      double offcut = double.Parse(offcutsData[0]);
      offcuts.Add(offcut);


      string[] values = data.Split(new string[] { "<", ">" }, StringSplitOptions.RemoveEmptyEntries);
      double timberA = double.Parse(values[1].Split(',')[1]);
      double timberB = Double.NaN;
      int count = data.Count(x => x == '<');
      if (count == 2)
      {
        timberB = double.Parse(values[2].Split(',')[1]);
      }
      timberALs.Add(timberA);
      timberBLs.Add(timberB);

    }
  }

  private string ParseString(string input, string separator1, string separator2)
  {
    // Extract the value inside the separators
    int startIndex = input.IndexOf(separator1) + 1;
    int endIndex = input.IndexOf(separator2, startIndex);
    string value = input.Substring(startIndex, endIndex - startIndex);
    return value;
  }


  private int CountOffcuts(List<double> offcuts)
  {
    int count = 0;
    foreach (var cut in offcuts)
    {
      if (cut != 0)
      {
        count++;
      }
    }
    return count;
  }
  #endregion
}