using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using MdXaml;
using WpfMath.Parsers;
using WpfMath.Rendering;
using XamlMath;
using HAP = HtmlAgilityPack;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfFontFamily = System.Windows.Media.FontFamily;

namespace VisionGrabber.Utilities
{
    public class PreviewRenderer
    {
        private static readonly Markdown _markdownEngine = new Markdown();

        /// <summary>
        /// Renders a Markdown string into a FlowDocument with dark theme styling and math support.
        /// </summary>
        /// <param name="markdown">The Markdown text to render.</param>
        /// <returns>A FlowDocument containing the rendered content.</returns>
        public static FlowDocument RenderDocument(string markdown)
        {
            var doc = new FlowDocument();
            doc.Background = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#1e1e1e"));
            doc.Foreground = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#d4d4d4"));
            doc.FontFamily = new WpfFontFamily("Segoe UI");
            doc.FontSize = 14;
            doc.PagePadding = new Thickness(15);
            doc.TextAlignment = TextAlignment.Left;
            doc.IsOptimalParagraphEnabled = false;

            if (string.IsNullOrEmpty(markdown))
            {
                doc.Blocks.Add(new Paragraph(new Run("(empty)")));
                return doc;
            }

            try
            {
                // Split the content by HTML tables to handle them separately from Markdown
                var parts = Regex.Split(markdown, @"(<table[\s\S]*?</table>)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                foreach (var part in parts)
                {
                    if (string.IsNullOrWhiteSpace(part)) continue;

                    if (part.TrimStart().StartsWith("<table", StringComparison.OrdinalIgnoreCase))
                    {
                        var tableBlock = CreateHtmlTableBlock(part);
                        if (tableBlock != null)
                        {
                            doc.Blocks.Add(tableBlock);
                        }
                        else
                        {
                            ProcessMarkdownAndMath(doc.Blocks, part);
                        }
                    }
                    else
                    {
                        ProcessMarkdownAndMath(doc.Blocks, part);
                    }
                }

                if (doc.Blocks.Count == 0)
                {
                    doc.Blocks.Add(new Paragraph(new Run("(empty)")));
                }
            }
            catch (Exception ex)
            {
                var errorDoc = new FlowDocument();
                errorDoc.Blocks.Add(new Paragraph(new Run("Preview error: " + ex.Message)));
                return errorDoc;
            }

            return doc;
        }

        private static void ProcessMarkdownAndMath(BlockCollection blocks, string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            var displayMathPattern = new Regex(@"\$\$(.+?)\$\$", RegexOptions.Singleline);
            var matches = displayMathPattern.Matches(text);

            int lastEnd = 0;
            foreach (Match match in matches)
            {
                if (match.Index > lastEnd)
                {
                    string beforeText = text.Substring(lastEnd, match.Index - lastEnd);
                    AddMarkdownSection(blocks, beforeText);
                }

                string latex = match.Groups[1].Value;
                var latexBlock = CreateLatexBlock(latex);
                blocks.Add(latexBlock);

                lastEnd = match.Index + match.Length;
            }

            if (lastEnd < text.Length)
            {
                string afterText = text.Substring(lastEnd);
                AddMarkdownSection(blocks, afterText);
            }
        }

        private static void AddMarkdownSection(BlockCollection blocks, string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = text.Split('\n');

            var mdBuffer = new System.Text.StringBuilder();
            bool inCodeBlock = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.TrimStart();

                if (trimmed.StartsWith("```"))
                {
                    inCodeBlock = !inCodeBlock;
                    mdBuffer.AppendLine(line);
                    continue;
                }

                if (inCodeBlock)
                {
                    mdBuffer.AppendLine(line);
                    continue;
                }

                bool isMarkdownStructure =
                    trimmed.StartsWith("#") ||
                    trimmed.StartsWith("- ") ||
                    trimmed.StartsWith("* ") ||
                    trimmed.StartsWith("+ ") ||
                    trimmed.StartsWith(">") ||
                    Regex.IsMatch(trimmed, @"^\d+\.\s") ||
                    Regex.IsMatch(trimmed, @"^---+$");

                if (isMarkdownStructure)
                {
                    mdBuffer.AppendLine(line);
                }
                else
                {
                    if (mdBuffer.Length > 0)
                    {
                        RenderMarkdownBuffer(blocks, mdBuffer.ToString());
                        mdBuffer.Clear();
                    }
                    RenderRawLine(blocks, line);
                }
            }

            if (mdBuffer.Length > 0)
            {
                RenderMarkdownBuffer(blocks, mdBuffer.ToString());
            }
        }

        /// <summary>
        /// Renders a buffer of Markdown text using the Markdown engine.
        /// </summary>
        private static void RenderMarkdownBuffer(BlockCollection blocks, string markdown)
        {
            try
            {
                var mdDoc = _markdownEngine.Transform(markdown);
                while (mdDoc.Blocks.Count > 0)
                {
                    var block = mdDoc.Blocks.FirstBlock;
                    mdDoc.Blocks.Remove(block);
                    ProcessInlineMath(block);
                    ApplyDarkThemeToBlock(block);
                    blocks.Add(block);
                }
            }
            catch
            {
                blocks.Add(new Paragraph(new Run(markdown)));
            }
        }

        /// <summary>
        /// Renders a raw line of text, processing inline math if present.
        /// </summary>
        private static void RenderRawLine(BlockCollection blocks, string line)
        {
            var para = new Paragraph();
            para.Margin = new Thickness(0);
            para.Foreground = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#d4d4d4"));
            para.FontFamily = new WpfFontFamily("Segoe UI");

            string pattern = @"(\$[^\$]*\$)";
            var parts = Regex.Split(line, pattern);

            foreach (var part in parts)
            {
                if (part.StartsWith("$") && part.EndsWith("$"))
                {
                    string latex = part.Trim('$');
                    if (string.IsNullOrWhiteSpace(latex))
                    {
                        para.Inlines.Add(new Run(part));
                    }
                    else
                    {
                        para.Inlines.Add(CreateInlineLatex(latex));
                    }
                }
                else if (!string.IsNullOrEmpty(part))
                {
                    string processed = part.Replace("\t", "    ");
                    processed = processed.Replace(" ", "\u00A0");
                    para.Inlines.Add(new Run(processed));
                }
            }

            if (para.Inlines.Count == 0)
            {
                para.Inlines.Add(new Run("\u00A0"));
            }

            blocks.Add(para);
        }

        /// <summary>
        /// Creates a WPF Table block from an HTML table string.
        /// </summary>
        private static Table CreateHtmlTableBlock(string html)
        {
            try
            {
                var htmlDoc = new HAP.HtmlDocument();
                htmlDoc.LoadHtml(html);

                var tableNode = htmlDoc.DocumentNode.SelectSingleNode("//table");
                if (tableNode == null) return null;

                var table = new Table();
                table.Background = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#252526"));
                table.BorderBrush = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#3e3e42"));
                table.BorderThickness = new Thickness(1);
                table.CellSpacing = 0;

                var rowGroup = new TableRowGroup();
                table.RowGroups.Add(rowGroup);

                var rows = tableNode.SelectNodes(".//tr");
                if (rows != null)
                {
                    foreach (var rowNode in rows)
                    {
                        var tr = new TableRow();
                        var cells = rowNode.SelectNodes(".//th|.//td");
                        if (cells != null)
                        {
                            foreach (var cellNode in cells)
                            {
                                var cell = new TableCell();
                                cell.BorderBrush = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#3e3e42"));
                                cell.BorderThickness = new Thickness(1);
                                cell.Padding = new Thickness(8);

                                string cellContent = cellNode.InnerHtml;
                                cellContent = cellContent.Replace("<br>", "\n").Replace("<br/>", "\n");
                                cellContent = System.Net.WebUtility.HtmlDecode(cellContent);

                                ProcessMarkdownAndMath(cell.Blocks, cellContent);

                                if (cellNode.Name == "th")
                                {
                                    cell.FontWeight = FontWeights.Bold;
                                    cell.Background = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#333333"));
                                }

                                tr.Cells.Add(cell);
                            }
                        }
                        rowGroup.Rows.Add(tr);
                    }
                }
                return table;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Recursively searches for and processes inline math within a Block.
        /// </summary>
        private static void ProcessInlineMath(Block block)
        {
            if (block is Paragraph para)
            {
                ProcessInlinesForMath(para.Inlines);
            }
            else if (block is List list)
            {
                foreach (var item in list.ListItems)
                {
                    foreach (var itemBlock in item.Blocks)
                    {
                        ProcessInlineMath(itemBlock);
                    }
                }
            }
            else if (block is Table table)
            {
                foreach (var rowGroup in table.RowGroups)
                {
                    foreach (var row in rowGroup.Rows)
                    {
                        foreach (var cell in row.Cells)
                        {
                            foreach (var cellBlock in cell.Blocks)
                            {
                                ProcessInlineMath(cellBlock);
                            }
                        }
                    }
                }
            }
            else if (block is Section section)
            {
                foreach (var sectionBlock in section.Blocks)
                {
                    ProcessInlineMath(sectionBlock);
                }
            }
        }

        /// <summary>
        /// Searches for and replaces inline math patterns ($...$) within an InlineCollection.
        /// </summary>
        private static void ProcessInlinesForMath(InlineCollection inlines)
        {
            var inlineMathPattern = new Regex(@"\$([^\$\n]+?)\$");
            var runsToProcess = new System.Collections.Generic.List<(Run run, System.Collections.Generic.List<Inline> replacements)>();

            foreach (var inline in inlines.ToList())
            {
                if (inline is Run run && run.Text != null)
                {
                    var matches = inlineMathPattern.Matches(run.Text);
                    if (matches.Count > 0)
                    {
                        var newInlines = new System.Collections.Generic.List<Inline>();
                        int lastEnd = 0;

                        foreach (Match match in matches)
                        {
                            if (match.Index > lastEnd)
                            {
                                string beforeText = run.Text.Substring(lastEnd, match.Index - lastEnd);
                                newInlines.Add(new Run(beforeText));
                            }

                            string latex = match.Groups[1].Value;
                            var mathInline = CreateInlineLatex(latex);
                            newInlines.Add(mathInline);

                            lastEnd = match.Index + match.Length;
                        }

                        if (lastEnd < run.Text.Length)
                        {
                            string afterText = run.Text.Substring(lastEnd);
                            newInlines.Add(new Run(afterText));
                        }

                        runsToProcess.Add((run, newInlines));
                    }
                }
                else if (inline is Span span)
                {
                    ProcessInlinesForMath(span.Inlines);
                }
            }

            foreach (var (run, replacements) in runsToProcess)
            {
                var parent = run.Parent as Paragraph;
                if (parent != null)
                {
                    var runInlines = parent.Inlines;
                    Inline nextInline = null;
                    bool foundRun = false;
                    foreach (var inline in runInlines.ToList())
                    {
                        if (foundRun) { nextInline = inline; break; }
                        if (inline == run) foundRun = true;
                    }

                    runInlines.Remove(run);

                    if (nextInline != null)
                    {
                        foreach (var newInline in replacements) runInlines.InsertBefore(nextInline, newInline);
                    }
                    else
                    {
                        foreach (var newInline in replacements) runInlines.Add(newInline);
                    }
                }
            }
        }

        /// <summary>
        /// Creates an inline LaTeX math element.
        /// </summary>
        private static Inline CreateInlineLatex(string latex)
        {
            try
            {
                var parser = WpfTeXFormulaParser.Instance;
                string coloredLatex = $"\\color[HTML]{{d4d4d4}}{{{latex.Trim()}}}";
                var formula = parser.Parse(coloredLatex);

                var environment = WpfTeXEnvironment.Create(TexStyle.Text, 14.0, "Segoe UI");
                var geometry = formula.RenderToGeometry(environment);

                var path = new System.Windows.Shapes.Path
                {
                    Data = geometry,
                    Fill = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#d4d4d4")),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var container = new InlineUIContainer(path);
                container.BaselineAlignment = BaselineAlignment.Center;
                path.Margin = new Thickness(1, 0, 1, -3);

                return container;
            }
            catch
            {
                return new Run($"[${latex}$]")
                {
                    Foreground = WpfBrushes.Orange,
                    FontFamily = new WpfFontFamily("Consolas")
                };
            }
        }

        /// <summary>
        /// Creates a display-mode LaTeX math block.
        /// </summary>
        private static Block CreateLatexBlock(string latex)
        {
            try
            {
                var parser = WpfTeXFormulaParser.Instance;
                string coloredLatex = $"\\color[HTML]{{d4d4d4}}{{{latex.Trim()}}}";
                var formula = parser.Parse(coloredLatex);

                var environment = WpfTeXEnvironment.Create(TexStyle.Display, 20.0, "Segoe UI");
                var geometry = formula.RenderToGeometry(environment);

                var path = new System.Windows.Shapes.Path
                {
                    Data = geometry,
                    Fill = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#d4d4d4")),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                };

                var container = new BlockUIContainer(path);
                ((System.Windows.Shapes.Path)container.Child).Margin = new Thickness(0, 10, 0, 10);

                return container;
            }
            catch (Exception ex)
            {
                var para = new Paragraph(new Run($"[LaTeX Error: {latex}] - {ex.Message}"));
                para.Foreground = WpfBrushes.Orange;
                para.FontFamily = new WpfFontFamily("Consolas");
                return para;
            }
        }

        /// <summary>
        /// Applies dark theme colors and default margins to a Block element.
        /// </summary>
        private static void ApplyDarkThemeToBlock(Block block)
        {
            block.Foreground = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#d4d4d4"));

            if (block is Paragraph para)
            {
                para.Margin = new Thickness(0);
                foreach (var inline in para.Inlines) ApplyDarkThemeToInline(inline);
            }
            else if (block is List list)
            {
                foreach (var item in list.ListItems)
                    foreach (var itemBlock in item.Blocks) ApplyDarkThemeToBlock(itemBlock);
            }
            else if (block is Table table)
            {
                table.Background = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#252526"));
                table.BorderBrush = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#3e3e42"));
                table.BorderThickness = new Thickness(1);

                foreach (var rowGroup in table.RowGroups)
                    foreach (var row in rowGroup.Rows)
                        foreach (var cell in row.Cells)
                        {
                            cell.BorderBrush = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#3e3e42"));
                            cell.BorderThickness = new Thickness(1);
                            cell.Padding = new Thickness(8);
                            foreach (var cellBlock in cell.Blocks) ApplyDarkThemeToBlock(cellBlock);
                        }
            }
            else if (block is Section section)
            {
                foreach (var sectionBlock in section.Blocks) ApplyDarkThemeToBlock(sectionBlock);
            }
        }

        /// <summary>
        /// Applies dark theme colors to an Inline element.
        /// </summary>
        private static void ApplyDarkThemeToInline(Inline inline)
        {
            inline.Foreground = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#d4d4d4"));

            if (inline is Span span)
            {
                if (inline.GetType().Name.Contains("Code") || span.FontFamily?.Source == "Consolas")
                {
                    span.Background = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#101010"));
                }
                foreach (var child in span.Inlines) ApplyDarkThemeToInline(child);
            }
        }
    }
}
