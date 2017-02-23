using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

using DataTransformer.Models;
using System.Text.RegularExpressions;

namespace DataTransformer
{
    public class DataConverter
    {
        public DataTable ConvertData(DataTable tblItems, List<TransfRule> transfRules)
        {
            foreach (DataRow rowItem in tblItems.Rows)
            {
                foreach (TransfRule rule in transfRules)
                {
                    bool transform = true;

                    try
                    {
                        switch (rule.IfOp)
                        {
                            case LogicOp.Is:
                                {
                                    if (!rowItem[rule.IfCol].ToString().Equals(rule.IfValue, StringComparison.OrdinalIgnoreCase)) { transform = false; }
                                }
                                break;
                            case LogicOp.Contains:
                                {
                                    if (rowItem[rule.IfCol].ToString().IndexOf(rule.IfValue, StringComparison.OrdinalIgnoreCase) == -1) { transform = false; }
                                }
                                break;
                            case LogicOp.Starts_With:
                                {
                                    if (!rowItem[rule.IfCol].ToString().StartsWith(rule.IfValue, StringComparison.OrdinalIgnoreCase)) { transform = false; }
                                }
                                break;
                            case LogicOp.Ends_With:
                                {
                                    if (!rowItem[rule.IfCol].ToString().EndsWith(rule.IfValue, StringComparison.OrdinalIgnoreCase)) { transform = false; }
                                }
                                break;
                            case LogicOp.Is_Greater_Than:
                                {
                                    if (rowItem[rule.IfCol].ToString().Length == 0) { rowItem[rule.IfCol] = "0"; }

                                    double colValue = double.Parse(rowItem[rule.IfCol].ToString());

                                    if (colValue <= double.Parse(rule.IfValue)) { transform = false; }

                                }
                                break;
                            case LogicOp.Is_Less_Than:
                                {
                                    if (rowItem[rule.IfCol].ToString().Length == 0) { rowItem[rule.IfCol] = "0"; }

                                    double colValue = double.Parse(rowItem[rule.IfCol].ToString());

                                    if (colValue >= double.Parse(rule.IfValue)) { transform = false; }
                                }
                                break;
                        }
                    }
                    catch(Exception ex)
                    {
                        throw new Exception(ex.Message + " (" + rowItem[rule.IfCol].ToString() + " " + rule.IfOp + " " + rule.IfValue + ")");
                    }

                    if (transform == true)
                    {
                        string convTo = rule.TransfTo;

                        // replace embedded columns with their values
                        if (rule.HasEmbdCols)
                        {
                            foreach (EmbeddedColumn embdCol in rule.EmbdCols)
                            {
                                convTo = convTo.Replace(embdCol.EmbdColName, rowItem[embdCol.ColumnName].ToString());
                            }
                        }

                        // perform math
                        if (convTo.IndexOfAny(new char[] { '*', '/', '+', '-'}, 2) > 0)
                        {
                            MatchCollection matches = Constants.rgxEmbeddedMath.Matches(convTo);

                            while (matches.Count > 0)
                            {   
                                foreach (Match match in matches)
                                {
                                    convTo = convTo.Replace(match.Value, EvaluateMath(match.Value));
                                }

                                matches = Constants.rgxEmbeddedMath.Matches(convTo);
                            }
                        }

                        rowItem[rule.TgtCol] = convTo;
                    }
                }
            }

            return tblItems;
        }

        /// <summary>
        /// Evaluates simple numeric formula. e.g. (9*0.10)
        /// </summary>
        /// <param name="formaula"></param>
        /// <returns></returns>
        private string EvaluateMath(string formula)
        {
            if (formula.Length < 5) { return formula; }

            formula = formula.Substring(1, formula.Length - 2).Replace(" ", "");

            int opPos = formula.IndexOfAny(new char[] { '*', '/', '+', '-' }, 1);

            formula = ExecuteSubMath(formula, formula[opPos], opPos);

            int decimalPos = formula.IndexOf('.');

            if (decimalPos > 0 && formula.Length > (decimalPos + 3))
            {
                formula = formula.Substring(0, decimalPos + 3); // take only up to 2 decimal places
            }

            return formula;
        }

        private string ExecuteSubMath(string formula, char oper, int operatorPos)
        {
            int start, end;
            string strNum1, strNum2;
            float num1, num2;
            double result = 0;

            while (operatorPos > 0)
            {
                start = operatorPos - 1;

                bool isNumber = false;

                for (; start >= 0; start--)
                {
                    if ((formula[start] >= '0' && formula[start] <= '9') ||
                         formula[start] == '.' || formula[start] == ',')
                    {
                        isNumber = true;
                    }
                    else if (formula[start] == ' ')
                    {
                        if (isNumber)
                        {
                            isNumber = false;
                            break;
                        }
                    }
                    else
                    {
                        if (formula[start] == '*' || formula[start] == '/' ||
                            formula[start] == '+' || formula[start] == '-')
                        {
                            if (formula[start] == '-' && start == 0) { /* it is negative number */	}
                            else { break; }
                        }
                        else
                        {
                            isNumber = false;
                            break;
                        }
                    }
                }

                if (!isNumber) { break; }

                strNum1 = formula.Substring(start + 1, operatorPos - start - 1);

                isNumber = false;

                end = operatorPos + 1;

                for (; end < formula.Length; end++)
                {
                    if ((formula[end] >= '0' && formula[end] <= '9') ||
                         formula[end] == '.' || formula[end] == ',')
                    {
                        isNumber = true;
                    }
                    else if (formula[end] == ' ')
                    {
                        if (isNumber)
                        {
                            isNumber = false;
                            break;
                        }
                    }
                    else
                    {
                        if (formula[end] == '*' || formula[end] == '/' ||
                             formula[end] == '+' || formula[end] == '-')
                        {
                            if (formula[end] == '-' && end == (operatorPos + 1)) { /* it is negative number */	}
                            else { break; }
                        }
                        else
                        {
                            isNumber = false;
                            break;
                        }
                    }
                }

                if (!isNumber) { break; }

                strNum2 = formula.Substring(operatorPos + 1, end - operatorPos - 1);

                if (!float.TryParse(strNum1, out num1))
                {
                    throw new Exception("Invalid operand1=" + strNum1 + " in formula=" + formula + " with operator=" + oper);
                }

                if (!float.TryParse(strNum2, out num2))
                {
                    throw new Exception("Invalid operand2=" + strNum2 + " in formula=" + formula + " with operator=" + oper);
                }

                if (oper == '*') { result = Math.Round(num1 * num2, 4); }
                else if (oper == '/') { result = Math.Round(num1 / num2, 4); }
                else if (oper == '+') { result = Math.Round(num1 + num2, 4); }
                else if (oper == '-') { result = Math.Round(num1 - num2, 4); }

                Regex regex = new Regex(Regex.Escape(strNum1 + oper.ToString() + strNum2));
                formula = regex.Replace(formula, result.ToString("0.######"), 1);

                operatorPos = formula.IndexOf(oper, 1);
            }

            return formula;
        }
    }
}