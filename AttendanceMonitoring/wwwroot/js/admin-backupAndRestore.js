//function confirmBackup() {
//    return confirm('Are you sure you want to create a new database backup? This may take a few minutes.');
//}

//function confirmRestore(fileName, createdDate) {

//    var confirm1 = confirm(
//        '⚠️ WARNING: DATABASE RESTORE\n\n' +
//        'You are about to RESTORE the database from:\n' +
//        fileName + '\n' +
//        'Created: ' + createdDate + '\n\n' +
//        '❌ This will DELETE all current data!\n' +
//        '❌ All changes after this backup will be LOST!\n\n' +
//        'A safety backup will be created first.\n\n' +
//        'Do you want to continue?'
//    );

//    if (!confirm1) {
//        return false;
//    }

//    var confirm2 = confirm(
//        '⚠️ FINAL WARNING!\n\n' +
//        'This action CANNOT be undone!\n\n' +
//        'All data added/modified after ' + createdDate + ' will be PERMANENTLY LOST!\n\n' +
//        'Click OK ONLY if you are absolutely sure!'
//    );

//    if (!confirm2) {
//        return false;
//    }

//    var userInput = prompt(
//        '⚠️ Type "RESTORE" (all caps) to confirm:\n\n' +
//        'This is your last chance to cancel!'
//    );

//    if (userInput !== 'RESTORE') {
//        alert('❌ Restore cancelled. Database was not modified.');
//        return false;
//    }

//    alert('Restore will now proceed. Please wait...');
//    return true;
//}

setTimeout(function () {
    var alerts = document.querySelectorAll('.alert-dismissable');
    alerts.forEach(function (alert) {
        var bsAlert = new bootstrap.Alert(alert);
        bsAlert.close();
    });
}, 5000);

$(document).ready(function () {
    $('#confirmBackupButton').on('click', function (e) {
        e.preventDefault();
        $('#backupForm').submit();
      
    });

    //ganito gamitin , hindi  $('.btn-restore') kase nasa loob ng loop yung button
    $(document).on('click', '.btn-restore', function (e) {

        var $form = $(this).closest('form');
        var fileName = $(this).data('filename');
        var date = $(this).data('date');
        //var formId = '#restoreForm-' + fileName;

        console.log('fileName:', fileName);
        //console.log('formId:', formId);
        //console.log('form found:', $(formId).length); // dapat 1, hindi 0

        $('#restoreFileName').text(fileName);
        $('#restoreDate').text(date);

        $('#confirmRestoreButton').off('click').on('click', function () {
            $form.submit();
        });

        $('#ModalRestore').modal('show');
    });

   
});
