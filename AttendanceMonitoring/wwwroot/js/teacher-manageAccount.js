

$(document).ready(function () {

    $(document).on('submit', '#manageAccountForm', function (e) {
        e.preventDefault();

        var formData = new FormData(this);
        //var teacherID = userID;

        //loadPageBlur();

        $('#manageAccountForm .form-control').removeClass('border-danger');
        $('#manageAccountForm .validation-error-message').remove();

        $.ajax({
            url: '/Teacher/ManageTeacherAccount/',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            dataType: 'json',
            success: function (response) {
                //console.log('Full response:', response); // Add this line

                if (response.success) {
                    $('#ManageAccountModalForm').off('hide.bs.modal'); //Para i-disable ang blur effect for hiding modal
                    $('#ManageAccountModalForm').modal('hide');
                    loadSpinner();
                    showUpdateSuccessToast(response.message);
                    setTimeout(function () {
                        location.reload();
                    }, 2000);
                } else {

                    $.each(response.errors, function (key, value) {
                        if (value && value.length > 0) {
                            var inputElement = $('[name="' + key + '"]');
                            inputElement.addClass('border-danger');

                            var errorMessageElement = inputElement.next('.validation-error-message');
                            if (errorMessageElement.length > 0) {
                                errorMessageElement.text(value.join(', '));
                            } else {
                                $('<span class="text-danger validation-error-message">' + value.join(', ') + '</span>').insertAfter(inputElement);
                            }
                        }
                    });
                    //console.log('Errors:', response.errors); // Add this line
                    //console.log('Message:', response.message); // Add this line
                    //    showDangerToast(response);
                }
            },
            error: function (xhr, status, error) {
                console.error('Error updating user:', error);
                console.log('XHR Response:', xhr.responseText); // Para makita mo yung actual error
                loadSpinner();
                loadPageBlur();
                showDangerToast('An error occurred while updating the admin.');
            }
        });
    });
});