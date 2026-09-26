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
    this.staff,
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
  final String? staff;

  ListRow copyWith({String? staff}) {
    return ListRow(
      title: title,
      subtitle: subtitle,
      meta: meta,
      tag: tag,
      id: id,
      fatherName: fatherName,
      mobile: mobile,
      cnic: cnic,
      note: note,
      date: date,
      city: city,
      staff: staff ?? this.staff,
    );
  }
}

/// Backward-compatible alias used by list/form screens.
typedef DummyRow = ListRow;
