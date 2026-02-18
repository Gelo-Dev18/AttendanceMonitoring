document.addEventListener("DOMContentLoaded", function () {
    $('#ViewModal').on('show.bs.modal', function () {
        $(this).data('has-assignments', false);
    });

    //This will reload the page for MyClasses list if there is a new assign
    $('#ViewModal').on('hide.bs.modal', function () {
        if ($(this).data('has-assignments')) {
            location.reload();
        }
    });
    //Submit Function For Adding Teacher
    $(document).on('submit', '#AddTeacherForm', function (e) {
        e.preventDefault(); // stops the default page refresh/navigation on form submission, allowing JavaScript to handle the data submission.

        var formData = new FormData(this); //collect and manage form data for submission
        //RESTful api, two computer system to exchange information through the internet
        $.ajax({
            url: '/Admin/AddTeacher',
            type: 'POST', //a RESTful API uses standard HTTP methods(GET, POST, PUT, DELETE) 
            data: formData, //sends all form data
            processData: false, // para hindi maconvert ni formdata to strings yung submission ng data lalo na if my file included
            contentType: false, //si browser mag set ng contenttype
            success: function (response) {
                //$('#AddTeacherModal').html(response); //after success it will return to Teacher List just like what's on the code in controller .  return PartialView("TeacherList");
                $('.form-control').removeClass('border-danger');
                $('.validation-error-message').text('');
                //$('#validationSum').empty();

                if (response.success) {
                    $('#AddModalForm').off('hide.bs.modal'); //Para i-disable ang blur effect for hiding modal
                    $('#AddModalForm').modal('hide');
                    // alert(response.message);
                    loadSpinner();
                    showSuccessToast(response.message);
                    setTimeout(function () {
                        location.reload();
                    }, 2000);
                } else {
                    //$('#AddTeacherModal').html(response);

                    //used to loop over collections of elements, such as arrays, objects, or jQuery objects(collections of DOM elements).
                    //Ang pinaka purpsoe ng code na ito is para gawing border-danger yung textbox na mayroong validation
                    //yung KEY is yung variable or fieldname ko like FirstName, PAssowrd, email etc,
                    //Yung value ayun yung validation error
                    $.each(response.errors, function (key, value) {
                        //dito kapag may error is if yung value.length is > 1 is mag rarun yung code kase may error pero pag wala mag skip ito
                        // yung .length is kung ilang yung element sa loob ng array (yung error) example var errors1 = ["Email is required", "Invalid format"];
                        if (value && value.length > 0) {
                            //Dito hinahanap nyo kung anong input field base sa name attribute. Yung name attribute is ayung "asp-for" so ginagamitan sya ng key kase ayun yung fieldname
                            //then pagnahanap na is lalagyan ng border-danger yung input text box nayun kapag may error
                            var inputElement = $('[name="' + key + '"]');
                            inputElement.addClass('border-danger');
                            //Hinahanap kung may existing error message na sa baba ng input
                            var errorMessageElement = inputElement.next('.validation-error-message');
                            //so dito kapag may error or validation na, is parang irereplace nalang nya ulit yung error kahit same para tapos  ididsplay para hindi mag dagdag or patong yung html element
                            if (errorMessageElement.length > 0) {
                                //so ayun kapag may error is ididsplay and replace lng para di mag patong or dumami error sa html element
                                errorMessageElement.text(value.join(', '));
                            } else {
                                //so dito kapag nag sumbit magrarun yung else code at lalagpas yung if conditon, if wala pang existing na error then maglalagay na ng error
                                //ngayon kapag nag submit tapos same error ulit, dun na tatakbo si IF condition
                                $('<span class="text-danger validation-error-message">' + value.join(', ') + '</span>').insertAfter(inputElement);
                            }
                        }

                    });

                    //$.each(response.errors, function (key, message) {
                    //    $('span[data-valmsg-for="' + key + '"]').text(message);
                    //});
                }
            },
            error: function (xhr, status, error) {
                console.error('Error saving teacher:', error);
                loadSpinner();
                loadPageBlur();
                showDangerToast('An error occurred while updating the teacher.');
            }
        });
    });
    //////////////////////////////////////////////////////////////

    //Submit Function for Edit Teacher
    $(document).on('submit', '#EditTeacherForm', function (e) {
        e.preventDefault();

        var formData = new FormData(this);
        //var teacherID = userID;

        //loadPageBlur();

        $('#EditTeacherForm .form-control').removeClass('border-danger');
        $('#EditTeacherForm .validation-error-message').remove();

        $.ajax({
            url: '/Admin/EditTeacher/' + userID,
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            dataType: 'json',
            success: function (response) { 
                //console.log('Full response:', response); // Add this line

                if (response.success) {
                    $('#EditModal').off('hide.bs.modal'); //Para i-disable ang blur effect for hiding modal
                    $('#EditModal').modal('hide');
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
                showDangerToast('An error occurred while updating the teacher.');
            }
        });
    });
    ///////////////////////////////////////////////////////////////////////////////

    //Submit Function for Edit Teacher
    $(document).on('click', '.assign-btn', function (e) {
        e.preventDefault();

        var teacherId = $(this).data("teacher-id");
        var sectionSubjectId = $(this).data("section-subject-id");

        $.ajax({
            url: '/Admin/AssignTeacher',
            type: 'POST',
            data: { teacherId: teacherId, sectionSubjectId: sectionSubjectId },
            //processData: false,
            //contentType: false,
            //dataType: 'json',
            success: function (response) {

                $('#AssignModal .modal-body').html(response);
                $('#assignTable').DataTable();

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
    ///////////////////////////////////////////////////////////////////////////////

    $('#confirmDeleteButton').on('click', function (e) {
        e.preventDefault();

        if (!userID) {
            alert('Id does not found');
            return;
        }

        //loadSpinner();
        //loadPageBlur(); //Para kapag nag success ang isang submit like adding, deleting or editing

        $.ajax({
            url: '/Admin/DeleteTeacher/' + userID,
            type: 'DELETE',
            success: function (response) {
                if (response.success) {

                    $('#ModalDelete').off('hide.bs.modal');// for quick fix only //Para i-disable ang blur effect for hiding modal
                    $('#ModalDelete').modal('hide');
                    showSuccessToast(response.message);
                    //loadSpinner();
                    setTimeout(function () {
                        location.reload();
                    }, 2000);
                } else {
                    showDangerToast(response.message)
                }
                //alert(response.message);
                //location.reload();
            },
            error: function (xhr, status, error) {
                console.error('Error deleting teacher:', error);
                //    alert('Something went wrong. Please try again.');
                loadSpinner();
                loadPageBlur();
                showDangerToast(response);
            }
        });
    });

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    $(document).on('click', '.removebtn', function (e) {
        e.preventDefault();
        //var button = $(this);
        //var button = $(event.relatedTarget); // ginagamit lang kapag modal

        var assignedToTeacherId = $(this).data('assign-id');

        $.ajax({
            url: '/Admin/RemoveAssignedToTeacher/' + assignedToTeacherId,
            type: 'DELETE',
            success: function (response) {
                $('#currentlyAssigned').html(response);
                $('#dataTable3').DataTable({
                    "autoWidth": false, // PREVENTION SA WIDE TABLE
                    "responsive": true,
                    "destroy": true
                });
                showUpdateSuccessToast("Remove Successfully");
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

    $(document).on('click', '.restore-btn', function (e) {
        e.preventDefault();

        var assignmentId = $(this).data("assignment-id");

        $.ajax({
            url: '/Admin/RestoreTeacherAssignment',
            type: 'POST',
            data: { assignmentId: assignmentId },
            //processData: false,
            //contentType: false,
            //dataType: 'json',
            success: function (response) {

                $('#ViewModal .modal-body').html(response);
                $('#dataTable2').DataTable();

                showUpdateSuccessToast("Restore Successfully!");
                $('#ViewModal').data('has-assignments', true);
            },
            error: function (xhr, status, error) {
                console.error('Error updating user:', error);
                console.log('XHR Response:', xhr.responseText); // Para makita mo yung actual error
                loadSpinner();
                loadPageBlur();
                showDangerToast('An error occurred while restoring section.');
            }
        });
    });

    $(document).on('click', '.restore-teacher-btn', function (e) {
        e.preventDefault();

        var teacherId = $(this).data("teacher-id");

        $.ajax({
            url: '/Admin/RestoreDeletedTeacher',
            type: 'POST',
            data: { teacherId: teacherId },
            //processData: false,
            //contentType: false,
            //dataType: 'json',
            success: function (response) {

                $('#ViewModal .modal-body').html(response);
                $('#dataTable2').DataTable();

                showUpdateSuccessToast("Restore Successfully!");
                $('#ViewModal').data('has-assignments', true);
            },
            error: function (xhr, status, error) {
                console.error('Error updating user:', error);
                console.log('XHR Response:', xhr.responseText); // Para makita mo yung actual error
                loadSpinner();
                loadPageBlur();
                showDangerToast('An error occurred while restoring student.');
            }
        });
    });

});