$(document).ready(function () {
    $(document).on('click', '.assign-btn', function (e) {
        e.preventDefault();

        var teacherId = $(this).data("teacher-id");
        var sectionSubjectId = $(this).data("section-subject-id");

        $.ajax({
            url: '/Teacher/SelfAssign',
            type: 'POST',
            data: { teacherId: teacherId, sectionSubjectId: sectionSubjectId },
            //processData: false,
            //contentType: false,
            //dataType: 'json',
            success: function (response) {

                $('#AssignModal .modal-body').html(response);
                $('#dataTable2').DataTable();

                showUpdateSuccessToast("Assigned Successfully!");
            },
            error: function (xhr, status, error) {
                console.error('Error updating user:', error);
                console.log('XHR Response:', xhr.responseText); // Para makita mo yung actual error
                loadSpinner();
                loadPageBlur();
                showDangerToast('An error occurred while updating the teacher.');
            }
        });
    });
});