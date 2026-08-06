/// Display DTO for module list rows (was DummyRow).
class ListRow {
  const ListRow({
    required this.title,
    required this.subtitle,
    required this.meta,
    this.tag,
    this.id,
    this.fatherName,
    this.mobile,
    this.cnic,
    this.note,
    this.date,
    this.city,
  });

  final int? id;
  final String title;
  final String subtitle;
  final String meta;
  final String? tag;
  final String? fatherName;
  final String? mobile;
  final String? cnic;
  final String? note;
  final String? date;
  final String? city;
}

/// Backward-compatible alias used by list/form screens.
typedef DummyRow = ListRow;
