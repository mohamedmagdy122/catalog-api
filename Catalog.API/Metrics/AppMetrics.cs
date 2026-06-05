using Prometheus;

namespace Catalog.API.Metrics;

public static class AppMetrics
{
    public static readonly Gauge ProductsTotal = Prometheus.Metrics
        .CreateGauge("catalog_products_total", "Total number of products in catalog.");

    public static readonly Counter ProductViewsTotal = Prometheus.Metrics
        .CreateCounter("catalog_product_views_total", "Total product views by category.",
            new CounterConfiguration { LabelNames = new[] { "category" } });

    public static readonly Histogram SearchDuration = Prometheus.Metrics
        .CreateHistogram("catalog_search_duration_seconds", "Duration of catalog search queries.",
            new HistogramConfiguration { Buckets = Histogram.ExponentialBuckets(0.001, 2, 12) });
}
