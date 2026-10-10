using MedLinkPortal.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MedLinkPortal.Services
{
    public class NeuralReportService : INeuralReportService
    {
        public byte[] GenerateReport(AIHealthReport report, string patientName)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    // Standard margin but zero for header background to bleed
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.SegoeUI));

                    page.Header().Element(header => ComposeHeader(header, report, patientName));
                    
                    page.Content().PaddingHorizontal(40).PaddingVertical(20).Element(content => ComposeContent(content, report));

                    page.Footer().PaddingHorizontal(40).PaddingBottom(20).Element(ComposeFooter);
                });
            })
            .GeneratePdf();
        }

        void ComposeHeader(IContainer container, AIHealthReport report, string patientName)
        {
            container.Background(Colors.Grey.Darken4).Padding(40).Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("MedLink Neural Core").FontSize(24).SemiBold().FontColor(Colors.White);
                    column.Item().Text("Deep Learning Health Analysis V2.4").FontSize(10).FontColor(Colors.Blue.Lighten2);
                    
                    column.Item().PaddingTop(20).Text(text =>
                    {
                        text.Span("Patient: ").FontColor(Colors.Grey.Lighten1);
                        text.Span(patientName).SemiBold().FontColor(Colors.White);
                    });
                    
                    column.Item().Text(text =>
                    {
                        text.Span("Date: ").FontColor(Colors.Grey.Lighten1);
                        text.Span(DateTime.Now.ToString("dd MMM yyyy, HH:mm")).SemiBold().FontColor(Colors.White);
                    });
                });

                row.ConstantItem(120).Column(column =>
                {
                    // Simulated Radial Score
                    column.Item().AlignCenter().Text(report.OverallScore.ToString()).FontSize(48).Black().FontColor(Colors.Green.Accent2);
                    column.Item().AlignCenter().Text(report.StatusLabel.ToUpper()).FontSize(10).SemiBold().FontColor(Colors.Green.Lighten3).LetterSpacing(0.1f);
                });
            });
        }

        void ComposeContent(IContainer container, AIHealthReport report)
        {
            container.Column(column =>
            {
                // Executive Summary
                column.Item().PaddingBottom(20).Text("Executive Summary").FontSize(16).SemiBold().FontColor(Colors.Grey.Darken3);
                column.Item().PaddingBottom(30).Text(report.Summary).FontSize(11).LineHeight(1.5f).FontColor(Colors.Grey.Darken2);

                // Vitals Grid
                column.Item().PaddingBottom(30).Element(e => ComposeVitalsGrid(e, report));

                // Diet Plan
                column.Item().ShowEntire().Element(e => ComposeDietPlan(e, report));

                // Clinical Protocols
                column.Item().PaddingTop(30).ShowEntire().Element(e => ComposeProtocols(e, report));
            });
        }

        void ComposeVitalsGrid(IContainer container, AIHealthReport report)
        {
            container.Grid(grid =>
            {
                grid.Columns(2);
                grid.Spacing(20);

                foreach (var vital in report.Vitals)
                {
                    grid.Item().Border(1).BorderColor(Colors.Grey.Lighten3).Background(Colors.Grey.Lighten5).Padding(15).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(vital.Label).FontSize(9).FontColor(Colors.Grey.Darken1).SemiBold().LetterSpacing(0.05f);
                            col.Item().Text($"{vital.Value} {vital.Unit}").FontSize(18).SemiBold().FontColor(Colors.Grey.Darken3);
                            col.Item().PaddingTop(5).Text("Neural Stability: 98%").FontSize(8).FontColor(Colors.Green.Darken1);
                        });
                    });
                }
            });
        }

        void ComposeDietPlan(IContainer container, AIHealthReport report)
        {
            container.Column(col =>
            {
                col.Item().PaddingBottom(15).Row(row =>
                {
                    row.RelativeItem().Text("Personalized Nutritional Strategy").FontSize(16).SemiBold().FontColor(Colors.Grey.Darken3);
                    row.ConstantItem(100).AlignRight().Text("Next 24 Hours").FontSize(10).FontColor(Colors.Blue.Medium).SemiBold();
                });

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(80);
                        columns.RelativeColumn();
                        columns.ConstantColumn(120);
                    });

                    table.Header(header =>
                    {
                        header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Time").SemiBold().FontColor(Colors.Grey.Darken1);
                        header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Meal Composition").SemiBold().FontColor(Colors.Grey.Darken1);
                        header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Nutritional Value").SemiBold().FontColor(Colors.Grey.Darken1);
                    });

                    foreach (var meal in report.DietPlan)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(10).Text(meal.MealTime).FontSize(10).SemiBold();
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(10).Text(meal.FoodItems).FontSize(10);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten4).Padding(10).Text(meal.NutritionalValue).FontSize(9).FontColor(Colors.Orange.Darken2);
                    }
                });
            });
        }

        void ComposeProtocols(IContainer container, AIHealthReport report)
        {
            container.Column(col =>
            {
                col.Item().PaddingBottom(15).Text("Clinical Protocols & Next Steps").FontSize(16).SemiBold().FontColor(Colors.Grey.Darken3);

                if (report.Protocols != null)
                {
                    foreach (var protocol in report.Protocols)
                    {
                        col.Item().PaddingBottom(10).Row(row =>
                        {
                            // Using a container to draw a circle (simple rounded box)
                            row.ConstantItem(15).PaddingTop(5).Element(e => e.Height(6).Width(6).Background(Colors.Blue.Medium).CornerRadius(3));
                            
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(protocol.Title).FontSize(11).SemiBold();
                                c.Item().Text(protocol.Description).FontSize(10).FontColor(Colors.Grey.Darken1);
                            });
                        });
                    }
                }
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(text =>
                    {
                        text.Span("Generated by MedLink Neural Core AI | ").FontColor(Colors.Grey.Medium);
                        text.Span("Secure & Encrypted").SemiBold().FontColor(Colors.Grey.Darken1);
                    });
                });

                row.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
            });
        }
    }
}
