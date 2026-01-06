function confirmBackup() {
    return confirm('Are you sure you want to create a new database backup? This may take a few minutes.');
}

setTimeout(function () {
    var alerts = document.querySelectorAll('.alert-dismissable');
    alerts.forEach(function (alert) {
        var bsAlert = new bootstrap.Alert(alert);
        bsAlert.close();
    });
});