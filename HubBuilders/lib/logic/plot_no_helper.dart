/// Same plot-number increment as C# `frmPlotInventoryFormAdd.NextPlotNo`.
String nextPlotNo(String current) {
  if (current.isEmpty) return '1';
  final asInt = int.tryParse(current);
  if (asInt != null) return '${asInt + 1}';

  final m = RegExp(r'^(.*?)(\d+)$').firstMatch(current);
  if (m != null) {
    final prefix = m.group(1)!;
    final digits = m.group(2)!;
    final num = int.parse(digits) + 1;
    return prefix + num.toString().padLeft(digits.length, '0');
  }
  return '$current-1';
}
