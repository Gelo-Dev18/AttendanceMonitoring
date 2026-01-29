// Call the dataTables jQuery plugin
$(document).ready(function() {
    $('#dataTable').DataTable();
    //$('#dataTable2').DataTable();
    $('#dataTable2').DataTable({
        "autoWidth": false, // PREVENTION SA WIDE TABLE
        "responsive": true,
        "destroy": true // SAFETY NET: Wasakin ang luma bago gumawa ng bago
    });
    //$('#assignSubject').DataTable();

});

//$(document).ready(function () {
//    $('#dataTable2').DataTable();
//    //$('#assignSubject').DataTable();

//});
//$(document).ready(function () {
//    $('#assignSubject').DataTable();
//});
