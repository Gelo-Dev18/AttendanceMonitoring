$(document).ready(function () {
    $('#dataTable4').DataTable();

    //1.Dito, naka-flag na false yung has-assignments so every time na mag open ang modal is naka false sya.
    //Mag automatic syang mag set na true kapag may assigned na bago kaya naka true na sya dun sa '.assign-btn',
    $('#AssignModal').on('show.bs.modal', function () {
        $(this).data('has-assignments', false);
    });

    //This will reload the page for MyClasses list if there is a new assign
    $('#AssignModal').on('hide.bs.modal', function () {
        if ($(this).data('has-assignments')) {
            location.reload();
        }
    });

    $(document).on('click', '.assign-btn', function (e) {
        e.preventDefault();

        var sectionSubjectId = $(this).data("section-subject-id");

        $.ajax({
            url: '/Teacher/SaveSelfAssign',
            type: 'POST',
            data: { sectionSubjectId: sectionSubjectId },

            success: function (response) {

                $('#AssignModal .modal-body').html(response);
                //$('#dataTable2').DataTable();
                $('#assignTable').DataTable();
                showUpdateSuccessToast("Assigned Successfully!");

                $('#AssignModal').data('has-assignments', true); //2. Dito na sya mag true kase nagamit yung assign button
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

    $(document).on('click', '.removebtn', function (e) {
        e.preventDefault();
        //var button = $(this);
        //var button = $(event.relatedTarget); // ginagamit lang kapag modal

        var assignId = $(this).data('assign-id');

        $.ajax({
            url: '/Teacher/RemoveSelfAssign/' + assignId,
            type: 'DELETE',
            success: function (response) {
                //$('#currentlyAssigned').html(response);
                if (response.success) {
                    showSuccessToast(response.message);
                    setTimeout(function () {
                        location.reload();
                    }, 2000);
                } else {
                    //showDangerToast(response.message)
                    showDangerToast(response.error)

                }       
            },
            error: function (xhr, status, error) {
                console.error('Error remove assign:', error);
                //    alert('Something went wrong. Please try again.');
                loadSpinner();
                loadPageBlur();
                showDangerToast(response);
            }
        });
    });
});