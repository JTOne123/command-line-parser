﻿
#region Using Directives

using Antlr4.Runtime.Misc;
using System.Collections.Generic;
using System.CommandLine.Parser.Antlr;
using System.CommandLine.Parser.Parameters;
using System.Globalization;
using System.Linq;

#endregion

namespace System.CommandLine.Parser
{
    /// <summary>
    /// Represents a visitor for the command line parser, which implements the visitor pattern to evaluate the parse tree generated by the parser.
    /// </summary>
    internal class CommandLineVisitor : CommandLineBaseVisitor<Parameter>
    {
        #region Private Static Fields

        /// <summary>
        /// Contains an american culture info, which is used for number conversion.
        /// </summary>
        private static CultureInfo americanCultureInfo = new CultureInfo("en-US");

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the parsed parameters.
        /// </summary>
        public Dictionary<string, Parameter> Parameters { get; private set; }

        /// <summary>
        /// Gets the parsed default parameters.
        /// </summary>
        public List<Parameter> DefaultParameters { get; private set; }

        #endregion

        #region CommandLineBaseListener Implementation

        /// <summary>
        /// Is called when the visitor reaches the start rule of the grammar.
        /// </summary>
        /// <param name="context">The context, which contains all information about the start rule of the grammar</param>
        /// <returns>Returns <c>null</c>, because nothing needs to be returned.</returns>
        public override Parameter VisitCommandLine([NotNull] Antlr.CommandLineParser.CommandLineContext context)
        {
            // Creates the result sets
            this.Parameters = new Dictionary<string, Parameter>();
            this.DefaultParameters = new List<Parameter>();

            // Visits the default parameters
            foreach (Antlr.CommandLineParser.DefaultParameterContext defaultParameterContext in context.defaultParameter())
                this.DefaultParameters.Add(this.Visit(defaultParameterContext));

            // Visits the parameters
            foreach (Antlr.CommandLineParser.ParameterContext parameterContext in context.parameter())
                this.Visit(parameterContext);

            // Nothing needs to be returned
            return null;
        }

        /// <summary>
        /// Is called when the visitor reaches a default parameter.
        /// </summary>
        /// <param name="context">The context, which contains all information about the default parameter that is being visited.</param>
        /// <returns>Returns the default parameter that was being visited.</returns>
        public override Parameter VisitDefaultParameterString([NotNull] Antlr.CommandLineParser.DefaultParameterStringContext context)
        {
            // Checks if the default parameter is a string or a quoted string
            string defaultParameterValue = null;
            if (context.String() != null)
                defaultParameterValue = context.String().GetText();
            if (context.QuotedString() != null)
                defaultParameterValue = context.QuotedString().GetText().Replace("\"", string.Empty);

            // Returns the parsed default parameter
            return new DefaultParameter { Value = defaultParameterValue };
        }

        /// <summary>
        /// Is called when the visitor reaches a boolean value.
        /// </summary>
        /// <param name="context">the context, which contains all information about the boolean value.</param>
        /// <returns>Returns the parameter with the boolean value.</returns>
        public override Parameter VisitBoolean([NotNull] Antlr.CommandLineParser.BooleanContext context) => new BooleanParameter { Value = context.True() != null ? true : false };

        /// <summary>
        /// Is called when the visitor reaches a number value.
        /// </summary>
        /// <param name="context">the context, which contains all information about the number value.</param>
        /// <returns>Returns the parameter with the number value.</returns>
        public override Parameter VisitNumber([NotNull] Antlr.CommandLineParser.NumberContext context) => new NumberParameter { Value = decimal.Parse(context.Number().GetText(), CommandLineVisitor.americanCultureInfo) };

        /// <summary>
        /// Is called when the visitor reaches a string value.
        /// </summary>
        /// <param name="context">the context, which contains all information about the string value.</param>
        /// <returns>Returns the parameter with the string value.</returns>
        public override Parameter VisitString([NotNull] Antlr.CommandLineParser.StringContext context)
        {
            // Checks if the string parameter is a string or a quoted string
            string parameterValue = null;
            if (context.String() != null)
                parameterValue = context.String().GetText();
            if (context.QuotedString() != null)
                parameterValue = context.QuotedString().GetText().Replace("\"", string.Empty);

            // Creates a new string parameter and returns it
            return new StringParameter { Value = parameterValue };
        }

        /// <summary>
        /// Is called when the visitor reaches an array value.
        /// </summary>
        /// <param name="context">the context, which contains all information about the array value.</param>
        /// <returns>Returns the parameter with the array value.</returns>
        public override Parameter VisitArray([NotNull] Antlr.CommandLineParser.ArrayContext context)
        {
            // Parses the content of the array
            List<Parameter> arrayContent = new List<Parameter>();
            for (int i = 0; i < context.ChildCount; i++)
            {
                Parameter newArrayContent = this.Visit(context.GetChild(i));
                if (newArrayContent != null)
                    arrayContent.Add(newArrayContent);
            }

            // Returns a new array parameter
            return new ArrayParameter { Value = arrayContent };
        }

        /// <summary>
        /// Is called when the visitor reaches a UNIX style flagged switch.
        /// </summary>
        /// <param name="context">The context, which contains all the information about the UNIX style flagged switch.</param>
        /// <returns>Returns <c>null</c> because the flags are directly added to the result set.</returns>
        public override Parameter VisitUnixStyleFlaggedSwitch([NotNull] Antlr.CommandLineParser.UnixStyleFlaggedSwitchContext context)
        {
            // Adds all the flags to the result set
            foreach (char flag in context.UnixStyleFlaggedIdentifiers().GetText().Replace("-", string.Empty))
            {
                string parameterName = flag.ToString();
                if (this.Parameters.ContainsKey(parameterName))
                    this.Parameters[parameterName] = new BooleanParameter { Value = true };
                else
                    this.Parameters.Add(parameterName, new BooleanParameter { Value = true });
            }

            // Nothings needs to be returned, because the flags are directly added to the result set
            return null;
        }

        /// <summary>
        /// Is called when the visitor reaches a UNIX style switch.
        /// </summary>
        /// <param name="context">The context, which contains all the information about the UNIX style switch.</param>
        /// <returns>Returns <c>null</c> because the switch is directly added to the result set.</returns>
        public override Parameter VisitUnixStyleSwitch([NotNull] Antlr.CommandLineParser.UnixStyleSwitchContext context)
        {
            // Adds the switch to the result set
            string parameterName = context.UnixStyleIdentifier().GetText().Replace("-", string.Empty);
            if (this.Parameters.ContainsKey(parameterName))
                this.Parameters[parameterName] = new BooleanParameter { Value = true };
            else
                this.Parameters.Add(parameterName, new BooleanParameter { Value = true });

            // Nothings needs to be returned, because the switch is directly added to the result set
            return null;
        }

        /// <summary>
        /// Is called when the visitor reaches a Windows style switch.
        /// </summary>
        /// <param name="context">The context, which contains all the information about the Windows style switch.</param>
        /// <returns>Returns <c>null</c> because the switch is directly added to the result set.</returns>
        public override Parameter VisitWindowsStyleSwitch([NotNull] Antlr.CommandLineParser.WindowsStyleSwitchContext context)
        {
            // Adds the switch to the result set
            string parameterName = context.WindowsStyleIdentifier().GetText().Replace("/", string.Empty);
            if (this.Parameters.ContainsKey(parameterName))
                this.Parameters[parameterName] = new BooleanParameter { Value = true };
            else
                this.Parameters.Add(parameterName, new BooleanParameter { Value = true });

            // Nothings needs to be returned, because the switch is directly added to the result set
            return null;
        }

        /// <summary>
        /// Is called when the visitor reaches a UNIX style parameter.
        /// </summary>
        /// <param name="context">The context, which contains all the information about the UNIX style parameter.</param>
        /// <returns>Returns <c>null</c> because the parameter is directly added to the result set.</returns>
        public override Parameter VisitUnixStyleParameter([NotNull] Antlr.CommandLineParser.UnixStyleParameterContext context)
        {
            // Parses the value of the parameter
            Parameter parameter = this.Visit(context.value());

            // Adds the parameter to the result set
            string parameterName = context.UnixStyleIdentifier().GetText().Replace("-", string.Empty);
            if (this.Parameters.ContainsKey(parameterName))
                this.Parameters[parameterName] = parameter;
            else
                this.Parameters.Add(parameterName, parameter);

            // Nothings needs to be returned, because the switch are directly added to the result set
            return null;
        }

        /// <summary>
        /// Is called when the visitor reaches a UNIX style alias parameter.
        /// </summary>
        /// <param name="context">The context, which contains all the information about the UNIX style alias parameter.</param>
        /// <returns>Returns <c>null</c> because the parameter is directly added to the result set.</returns>
        public override Parameter VisitUnixStyleAliasParameter([NotNull] Antlr.CommandLineParser.UnixStyleAliasParameterContext context)
        {
            // Parses the value of the parameter
            Parameter parameter = this.Visit(context.value());

            // Adds the parameters to the result set
            foreach (char flag in context.UnixStyleFlaggedIdentifiers().GetText().Replace("-", string.Empty))
            {
                string parameterName = flag.ToString();
                if (this.Parameters.ContainsKey(parameterName))
                    this.Parameters[parameterName] = parameter;
                else
                    this.Parameters.Add(parameterName, parameter);
            }

            // Nothings needs to be returned, because the switch are directly added to the result set
            return null;
        }

        /// <summary>
        /// Is called when the visitor reaches a Windows style parameter.
        /// </summary>
        /// <param name="context">The context, which contains all the information about the Windows style parameter.</param>
        /// <returns>Returns <c>null</c> because the parameter is directly added to the result set.</returns>
        public override Parameter VisitWindowsStyleParameter([NotNull] Antlr.CommandLineParser.WindowsStyleParameterContext context)
        {
            // Parses the value of the parameter
            Parameter parameter = this.Visit(context.value());

            // Adds the parameter to the result set
            string parameterName = context.WindowsStyleIdentifier().GetText().Replace("/", string.Empty);
            if (this.Parameters.ContainsKey(parameterName))
                this.Parameters[parameterName] = parameter;
            else
                this.Parameters.Add(parameterName, parameter);

            // Nothings needs to be returned, because the switch are directly added to the result set
            return null;
        }

        #endregion
    }
}