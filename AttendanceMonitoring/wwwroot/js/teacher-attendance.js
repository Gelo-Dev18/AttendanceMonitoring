var currentStudentId = null;

function checkAllAnswered() {
    let allAnswered = true;
    $('input[type="radio"]').each(function () {
        const name = $(this).attr('name');
        if ($('input[name="' + name + '"]:checked').length === 0) {
            allAnswered = false;
        }
    });
    return allAnswered;
}

$(document).ready(function () {
    $('input[type="radio"]').on('change', function () {
        if (checkAllAnswered()) {
            $('#submitBtn').removeAttr('disabled');
        } else {
            $('#submitBtn').attr('disabled', true);
        }
    });

    $(document).on('submit', '#SaveAttendanceForm', function (e) {
        e.preventDefault();

        var hasError = false;

        // I-check lahat ng excuse radio na naka-check
        $('.excuse-radio:checked').each(function () {
            var studentId = $(this).data('student-id');
            var studentName = $(this).data('student-name');
            var reason = $('input[name="ExcuseReason[' + studentId + ']"]').val();

            // Kung walang reason, error!
            if (!reason) {
                alert('Please provide a reason for ' + studentName + '\'s excuse.');
                hasError = true;
                return false; // Para lumabas sa .each() loop
            }
        });

        // Kung may error, STOP - huwag i-submit
        if (hasError) {
            return false;
        }

        var formData = new FormData(this);
        $.ajax({
            url: '/Teacher/SaveAttendance',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            dataType: 'json',
            success: function (response) {

                if (response.success) {
                    showSuccessToast(response.message);
                    loadSpinner();
                    loadPageBlur();
                    setTimeout(function () {
                        //Reset the form
                        document.getElementById('SaveAttendanceForm').reset();

                        //remove active class
                        var activeItems = document.querySelectorAll('#selectedValue .actve');
                        activeItems.forEach(function (item) {
                            item.classList.remove('active');
                        });

                        //location.reload();
                        //Relod page without the class section
                        //This will make model.Student = null
                        window.location.href = window.location.pathname;
                    }, 3000);
                } else {
                    alert('Could not remove assign');
                }
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

    // ============================================
    // PART 1: Kapag nag-click ng EXCUSE radio button
    // ============================================

    $('.excuse-radio').on('click', function () {
        // Kunin yung student ID at name
        currentStudentId = $(this).data('student-id');
        var studentName = $(this).data('student-name');
        console.log('Excuse clicked:', studentName); // For debugging

        // I-set yung name sa modal
        $('#modalStudentName').text(studentName);

        // Kunin yung existing reason (kung meron na)
        var existingReason = $('input[name="ExcuseReason[' + currentStudentId + ']"]').val();
        $('#modalExcuseReason').val(existingReason);

        // Buksan yung modal
        $('#excuseModal').modal('show');
    });

    // ============================================
    // PART 2: Kapag nag-click ng ibang radio (Present, Absent, Late)
    // ============================================
    $('.attendance-radio:not(.excuse-radio)').on('change', function () {
        var studentId = $(this).data('student-id');
        var row = $('tr[data-student-id="' + studentId + '"]');

        // I-clear yung excuse reason
        row.find('.excuse-reason-input').val('');
        row.find('.excuse-reason-display').html('').hide();
        row.removeClass('has-excuse');
    });


    // ============================================
    // PART 3: Kapag nag-click ng "Save Reason" button
    // ============================================
    $('#saveExcuseBtn').on('click', function () {
        var reason = $('#modalExcuseReason').val().trim();
        // I-check kung may laman
        if (!reason) {

            showDangerToast("Please enter a reason for the excuse.")
            return;
        }
        // Kunin yung row ng student
        var row = $('tr[data-student-id="' + currentStudentId + '"]');

        // I-save yung reason sa hidden input
        row.find('.excuse-reason-input').val(reason);

        // I-display yung badge indicator
        var shortReason = reason.length > 10 ? reason.substring(0, 10) + '...' : reason;
        row.find('.excuse-reason-display').html(
            '<span class="badge bg-info excuse-badge" title="' + reason + '">📝 ' + shortReason + '</span>'
        ).show();

        // I-highlight yung row
        row.addClass('has-excuse');

        // I-close yung modal
        $('#excuseModal').modal('hide');
    });


    // ============================================
    // PART 4: Kapag ni-cancel yung modal (walang save)
    // ============================================
    $('#excuseModal').on('hidden.bs.modal', function () {
        // I-check kung may existing reason na ba
        var reason = $('input[name="ExcuseReason[' + currentStudentId + ']"]').val();

        // Kung wala pang reason, i-uncheck yung excuse radio
        if (!reason) {
            $('#excuse_' + currentStudentId).prop('checked', false);
            $('#submitBtn').attr('disabled', true);

        }
    });


    // ============================================
    // PART 5: Validation bago mag-submit ng form
    // ============================================
    //$('#SaveAttendanceForm').on('submit', function (e) {
    //    var hasError = false;

    //    // I-check lahat ng excuse radio na naka-check
    //    $('.excuse-radio:checked').each(function () {
    //        var studentId = $(this).data('student-id');
    //        var studentName = $(this).data('student-name');
    //        var reason = $('input[name="ExcuseReason[' + studentId + ']"]').val();

    //        // Kung walang reason, error!
    //        if (!reason) {
    //            alert('Please provide a reason for ' + studentName + '\'s excuse.');
    //            hasError = true;
    //            return false; // Para lumabas sa .each() loop
    //        }
    //    });

    //    // Kung may error, huwag i-submit
    //    if (hasError) {
    //        e.preventDefault();
    //    }
    //});
});